using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models;

namespace server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public ContractsController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var contracts = await _db.Contracts
            .Include(c => c.Payments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(contracts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await _db.Contracts
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null) return NotFound();
        return Ok(contract);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] ContractCreateDto dto)
    {
        var contract = new Contract
        {
            Name = dto.Name,
            Description = dto.Description,
            TotalAmount = dto.TotalAmount,
            OriginalAmount = dto.TotalAmount
        };

        if (dto.File != null && dto.File.Length > 0)
        {
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "contracts");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await dto.File.CopyToAsync(stream);

            contract.FileName = dto.File.FileName;
            contract.FilePath = Path.Combine("uploads", "contracts", uniqueFileName);
        }

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = contract.Id }, contract);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContractUpdateDto dto)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract is null) return NotFound();

        contract.Name = dto.Name;
        contract.Description = dto.Description;
        contract.TotalAmount = dto.TotalAmount;
        contract.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(contract);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract is null) return NotFound();

        if (!string.IsNullOrEmpty(contract.FilePath))
        {
            var filePath = Path.Combine(_environment.ContentRootPath, contract.FilePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _db.Contracts.Remove(contract);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract is null) return NotFound();

        if (string.IsNullOrEmpty(contract.FilePath))
            return NotFound("合同文件不存在");

        var filePath = Path.Combine(_environment.ContentRootPath, contract.FilePath);
        if (!System.IO.File.Exists(filePath))
            return NotFound("文件不存在");

        var memory = new MemoryStream();
        await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            await stream.CopyToAsync(memory);
        }

        memory.Position = 0;
        return File(memory, "application/octet-stream", contract.FileName ?? "contract.bin");
    }

    [HttpPost("{id}/payments")]
    public async Task<IActionResult> AddPayment(int id, [FromBody] PaymentCreateDto dto)
    {
        var contract = await _db.Contracts
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null) return NotFound();

        if (contract.PaidAmount + dto.Amount > contract.TotalAmount)
        {
            return BadRequest("支付金额超过合同总额");
        }

        var payment = new Payment
        {
            ContractId = id,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            Note = dto.Note
        };

        _db.Payments.Add(payment);
        contract.PaidAmount += dto.Amount;
        contract.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(payment);
    }

    [HttpGet("{id}/payments")]
    public async Task<IActionResult> GetPayments(int id)
    {
        var payments = await _db.Payments
            .Where(p => p.ContractId == id)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return Ok(payments);
    }
}

public class ContractCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
    public IFormFile? File { get; set; }
}

public class ContractUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PaymentCreateDto
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
