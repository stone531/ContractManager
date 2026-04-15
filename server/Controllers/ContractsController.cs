using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models;
using System.Security.Claims;

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
    public async Task<IActionResult> GetAll([FromQuery] string? name, [FromQuery] string? number, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? approvalStatus)
    {
        var query = _db.Contracts
            .Include(c => c.Payments)
            .AsQueryable();

        // 按名称过滤
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(c => c.Name.Contains(name));
        }

        // 按编号过滤
        if (!string.IsNullOrWhiteSpace(number))
        {
            query = query.Where(c => c.ContractNumber != null && c.ContractNumber.Contains(number));
        }

        // 按创建日期范围过滤
        if (startDate.HasValue)
        {
            query = query.Where(c => c.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            // 包含结束日期当天的所有记录
            var endOfDay = endDate.Value.AddDays(1);
            query = query.Where(c => c.CreatedAt < endOfDay);
        }

        // 按审批状态过滤
        if (approvalStatus.HasValue)
        {
            query = query.Where(c => (int)c.ApprovalStatus == approvalStatus.Value);
        }

        var contracts = await query
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
        var currentUserId = GetCurrentUserId();
        var currentUser = currentUserId.HasValue ? await _db.Users.FindAsync(currentUserId.Value) : null;

        // 自动生成合同编号 HT + yyMMdd + 序号
        var today = DateTime.Now.ToString("yyMMdd");
        var count = await _db.Contracts
            .Where(c => c.ContractNumber != null && c.ContractNumber.StartsWith("HT" + today))
            .CountAsync();
        var contractNumber = $"HT{today}{(count + 1):D3}";

        // 超管创建直接通过，普通用户需审核
        var isSuperAdmin = currentUser?.Role == UserRole.SuperAdmin;

        var contract = new Contract
        {
            ContractNumber = contractNumber,
            Name = dto.Name,
            Description = dto.Description,
            TotalAmount = dto.TotalAmount,
            OriginalAmount = dto.TotalAmount,
            CreatedBy = currentUserId,
            ApprovalStatus = isSuperAdmin ? ApprovalStatus.Approved : ApprovalStatus.Pending
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

        // 如果非超管，通知所有超管
        if (!isSuperAdmin && currentUserId.HasValue)
        {
            var superAdmins = await _db.Users.Where(u => u.Role == UserRole.SuperAdmin).ToListAsync();
            foreach (var admin in superAdmins)
            {
                _db.Notifications.Add(new Notification
                {
                    Type = NotificationType.ContractApproval,
                    Message = $"用户 {currentUser?.Name} 创建了新合同「{contract.Name}」({contract.ContractNumber})，等待审批",
                    ContractId = contract.Id,
                    FromUserId = currentUserId.Value,
                    ToUserId = admin.Id
                });
            }
            await _db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = contract.Id }, contract);
    }

    // 获取待审核合同
    [HttpGet("pending-contracts")]
    public async Task<IActionResult> GetPendingContracts()
    {
        var contracts = await _db.Contracts
            .Where(c => c.ApprovalStatus == ApprovalStatus.Pending)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(contracts);
    }

    // 获取待审核金额
    [HttpGet("pending-amounts")]
    public async Task<IActionResult> GetPendingAmounts()
    {
        var contracts = await _db.Contracts
            .Where(c => c.SubmittedAmount > 0 && c.SubmittedBy != null)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
        return Ok(contracts);
    }

    // 审批合同通过
    [HttpPost("{id}/approve-contract")]
    public async Task<IActionResult> ApproveContract(int id)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();

        contract.ApprovalStatus = ApprovalStatus.Approved;
        contract.ApprovedAt = DateTime.UtcNow;

        // 通知创建者
        if (contract.CreatedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.ContractApproved,
                Message = $"您的合同「{contract.Name}」({contract.ContractNumber}) 已审批通过",
                ContractId = contract.Id,
                FromUserId = GetCurrentUserId() ?? 0,
                ToUserId = contract.CreatedBy.Value
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "合同审批通过" });
    }

    // 驳回合同
    [HttpPost("{id}/reject-contract")]
    public async Task<IActionResult> RejectContract(int id, [FromBody] RejectReasonDto dto)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();

        contract.ApprovalStatus = ApprovalStatus.Rejected;

        if (contract.CreatedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.ContractRejected,
                Message = $"您的合同「{contract.Name}」({contract.ContractNumber}) 已被驳回",
                ContractId = contract.Id,
                FromUserId = GetCurrentUserId() ?? 0,
                ToUserId = contract.CreatedBy.Value,
                RejectReason = dto.Reason
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "合同已驳回" });
    }

    // 提交金额审批
    [HttpPost("{id}/submit-amount")]
    public async Task<IActionResult> SubmitAmount(int id, [FromBody] SubmitAmountDto dto)
    {
        var currentUserId = GetCurrentUserId();
        var currentUser = currentUserId.HasValue ? await _db.Users.FindAsync(currentUserId.Value) : null;

        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();

        contract.SubmittedAmount = dto.Amount;
        contract.SubmittedBy = currentUserId;
        contract.UpdatedAt = DateTime.UtcNow;

        // 通知超管
        var superAdmins = await _db.Users.Where(u => u.Role == UserRole.SuperAdmin).ToListAsync();
        foreach (var admin in superAdmins)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.AmountApproval,
                Message = $"用户 {currentUser?.Name} 为合同「{contract.Name}」提交了金额 ¥{dto.Amount:F2}，等待审批",
                ContractId = contract.Id,
                FromUserId = currentUserId ?? 0,
                ToUserId = admin.Id
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "金额已提交审批" });
    }

    // 审批金额通过
    [HttpPost("{id}/approve-amount")]
    public async Task<IActionResult> ApproveAmount(int id)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();

        contract.TotalAmount = contract.SubmittedAmount;
        contract.SubmittedAmount = 0;
        var submittedBy = contract.SubmittedBy;
        contract.SubmittedBy = null;
        contract.UpdatedAt = DateTime.UtcNow;

        if (submittedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.AmountApproved,
                Message = $"合同「{contract.Name}」的金额变更已审批通过，新金额 ¥{contract.TotalAmount:F2}",
                ContractId = contract.Id,
                FromUserId = GetCurrentUserId() ?? 0,
                ToUserId = submittedBy.Value
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "金额审批通过" });
    }

    // 驳回金额
    [HttpPost("{id}/reject-amount")]
    public async Task<IActionResult> RejectAmount(int id, [FromBody] RejectReasonDto dto)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();

        var submittedBy = contract.SubmittedBy;
        contract.SubmittedAmount = 0;
        contract.SubmittedBy = null;
        contract.UpdatedAt = DateTime.UtcNow;

        if (submittedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.AmountRejected,
                Message = $"合同「{contract.Name}」的金额变更已被驳回",
                ContractId = contract.Id,
                FromUserId = GetCurrentUserId() ?? 0,
                ToUserId = submittedBy.Value,
                RejectReason = dto.Reason
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "金额已驳回" });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var id)) return id;
        return null;
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

public class SubmitAmountDto
{
    public decimal Amount { get; set; }
}

public class RejectReasonDto
{
    public string? Reason { get; set; }
}
