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

    // ────────── 辅助方法 ──────────

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var id)) return id;
        return null;
    }

    private async Task<(int userId, string userName, bool isSuperAdmin)> GetCurrentUserInfo()
    {
        var uid = GetCurrentUserId();
        if (!uid.HasValue) return (0, "Unknown", false);
        var user = await _db.Users.FindAsync(uid.Value);
        return (uid.Value, user?.Name ?? "Unknown", user?.Role == UserRole.SuperAdmin);
    }

    private void AddAuditLog(int? contractId, int userId, string userName, string action, string description)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ContractId = contractId,
            UserId = userId,
            UserName = userName,
            Action = action,
            Description = description
        });
    }

    // ────────── 今日统计 ──────────

    [HttpGet("today-stats")]
    public async Task<IActionResult> GetTodayStats()
    {
        var (userId, userName, isSuperAdmin) = await GetCurrentUserInfo();
        var todayUtc = DateTime.UtcNow.Date;

        // 今日新增合同统计（SQLite 不支持 decimal Sum，先拉到内存再求和）
        var todayNewContracts = await _db.Contracts.CountAsync(c => c.CreatedAt >= todayUtc);
        var todayContractAmounts = await _db.Contracts
            .Where(c => c.CreatedAt >= todayUtc)
            .Select(c => c.TotalAmount)
            .ToListAsync();
        var todayNewAmount = todayContractAmounts.Sum();

        // 今日已审批支付金额
        var todayPaidAmounts = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Approved && p.CreatedAt >= todayUtc)
            .Select(p => p.Amount)
            .ToListAsync();
        var todayPaidAmount = todayPaidAmounts.Sum();

        // 待审批数（仅超管）
        int todayPendingApprovals = 0;
        if (isSuperAdmin)
        {
            todayPendingApprovals += await _db.Contracts
                .CountAsync(c => c.ApprovalStatus == ApprovalStatus.Pending);
            todayPendingApprovals += await _db.Contracts
                .CountAsync(c => c.SubmittedAmount > 0 && c.SubmittedBy != null);
            todayPendingApprovals += await _db.Payments
                .CountAsync(p => p.Status == PaymentStatus.Pending);
        }

        var unreadNotifications = await _db.Notifications
            .CountAsync(n => n.ToUserId == userId && !n.IsRead);

        // 合同状态分布（数据库端 GroupBy）
        var totalContracts = await _db.Contracts.CountAsync();
        var statusGroups = await _db.Contracts
            .GroupBy(c => c.ContractStatus)
            .Select(g => new { Status = (int)g.Key, Count = g.Count() })
            .ToListAsync();

        var byStatus = new Dictionary<string, int> { { "0", 0 }, { "1", 0 }, { "2", 0 }, { "3", 0 } };
        foreach (var g in statusGroups)
        {
            byStatus[g.Status.ToString()] = g.Count;
        }

        return Ok(new
        {
            todayNewContracts,
            todayNewAmount,
            todayPaidAmount,
            todayPendingApprovals,
            unreadNotifications,
            totalContracts,
            byStatus
        });
    }

    // ────────── 查询 ──────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? name, [FromQuery] string? number,
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] int? approvalStatus, [FromQuery] int? contractStatus,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Contracts.Include(c => c.Payments).AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(c => c.Name.Contains(name));
        if (!string.IsNullOrWhiteSpace(number))
            query = query.Where(c => c.ContractNumber != null && c.ContractNumber.Contains(number));
        if (startDate.HasValue)
            query = query.Where(c => c.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(c => c.CreatedAt < endDate.Value.AddDays(1));
        if (approvalStatus.HasValue)
            query = query.Where(c => (int)c.ApprovalStatus == approvalStatus.Value);
        if (contractStatus.HasValue)
            query = query.Where(c => (int)c.ContractStatus == contractStatus.Value);

        var total = await query.CountAsync();

        var contracts = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { items = contracts, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await _db.Contracts.Include(c => c.Payments).FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        return Ok(contract);
    }

    // ────────── 创建合同 ──────────

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] ContractCreateDto dto)
    {
        var (userId, userName, isSuperAdmin) = await GetCurrentUserInfo();

        var contract = new Contract
        {
            ContractNumber = dto.ContractNumber,
            Name = dto.Name,
            Description = dto.Description,
            TotalAmount = dto.TotalAmount,
            OriginalAmount = dto.TotalAmount,
            CreatedBy = userId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ApprovalStatus = isSuperAdmin ? ApprovalStatus.Approved : ApprovalStatus.Pending,
            ContractStatus = isSuperAdmin ? ContractStatus.InProgress : ContractStatus.Initial
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

        AddAuditLog(contract.Id, userId, userName, "ContractCreated",
            $"创建合同「{contract.Name}」({contract.ContractNumber})，金额 ¥{contract.TotalAmount:F2}");

        if (!isSuperAdmin)
        {
            var superAdmins = await _db.Users.Where(u => u.Role == UserRole.SuperAdmin).ToListAsync();
            foreach (var admin in superAdmins)
            {
                _db.Notifications.Add(new Notification
                {
                    Type = NotificationType.ContractApproval,
                    Message = $"用户 {userName} 创建了新合同「{contract.Name}」({contract.ContractNumber})，等待审批",
                    ContractId = contract.Id,
                    FromUserId = userId,
                    ToUserId = admin.Id
                });
            }
        }

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = contract.Id }, contract);
    }

    // ────────── 编辑合同 ──────────

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContractUpdateDto dto)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract is null) return NotFound();
        if (contract.ContractStatus == ContractStatus.Terminated)
            return BadRequest("已终止的合同不可编辑");

        var (userId, userName, _) = await GetCurrentUserInfo();

        contract.Name = dto.Name;
        contract.Description = dto.Description;
        contract.TotalAmount = dto.TotalAmount;
        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.EndDate;
        contract.UpdatedAt = DateTime.UtcNow;

        AddAuditLog(contract.Id, userId, userName, "ContractEdited",
            $"编辑合同「{contract.Name}」，金额 ¥{contract.TotalAmount:F2}");

        await _db.SaveChangesAsync();
        return Ok(contract);
    }

    // ────────── 删除合同 ──────────

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, userName, isSuperAdmin) = await GetCurrentUserInfo();
        if (!isSuperAdmin) return Forbid();

        var contract = await _db.Contracts.FindAsync(id);
        if (contract is null) return NotFound();

        if (!string.IsNullOrEmpty(contract.FilePath))
        {
            var filePath = Path.Combine(_environment.ContentRootPath, contract.FilePath);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        AddAuditLog(contract.Id, userId, userName, "ContractDeleted",
            $"删除合同「{contract.Name}」({contract.ContractNumber})");

        _db.Contracts.Remove(contract);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ────────── 终止合同 ──────────

    [HttpPost("{id}/terminate")]
    public async Task<IActionResult> Terminate(int id)
    {
        var (userId, userName, isSuperAdmin) = await GetCurrentUserInfo();
        if (!isSuperAdmin) return Forbid();

        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();
        if (contract.ContractStatus == ContractStatus.Terminated)
            return BadRequest("合同已终止");

        contract.ContractStatus = ContractStatus.Terminated;
        contract.TerminatedAt = DateTime.UtcNow;
        contract.TerminatedBy = userId;
        contract.UpdatedAt = DateTime.UtcNow;

        AddAuditLog(contract.Id, userId, userName, "ContractTerminated",
            $"终止合同「{contract.Name}」({contract.ContractNumber})");

        // 通知创建者
        if (contract.CreatedBy.HasValue && contract.CreatedBy.Value != userId)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.ContractRejected,
                Message = $"合同「{contract.Name}」({contract.ContractNumber}) 已被终止",
                ContractId = contract.Id,
                FromUserId = userId,
                ToUserId = contract.CreatedBy.Value
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "合同已终止" });
    }

    // ────────── 合同审批 ──────────

    [HttpGet("pending-contracts")]
    public async Task<IActionResult> GetPendingContracts()
    {
        var contracts = await _db.Contracts
            .Where(c => c.ApprovalStatus == ApprovalStatus.Pending)
            .OrderByDescending(c => c.CreatedAt).ToListAsync();
        return Ok(contracts);
    }

    [HttpPost("{id}/approve-contract")]
    public async Task<IActionResult> ApproveContract(int id)
    {
        var (userId, userName, _) = await GetCurrentUserInfo();
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();

        contract.ApprovalStatus = ApprovalStatus.Approved;
        contract.ContractStatus = ContractStatus.InProgress;
        contract.ApprovedAt = DateTime.UtcNow;
        contract.UpdatedAt = DateTime.UtcNow;

        AddAuditLog(contract.Id, userId, userName, "ContractApproved",
            $"审批通过合同「{contract.Name}」({contract.ContractNumber})");

        if (contract.CreatedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.ContractApproved,
                Message = $"您的合同「{contract.Name}」({contract.ContractNumber}) 已审批通过",
                ContractId = contract.Id,
                FromUserId = userId,
                ToUserId = contract.CreatedBy.Value
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "合同审批通过" });
    }

    [HttpPost("{id}/reject-contract")]
    public async Task<IActionResult> RejectContract(int id, [FromBody] RejectReasonDto dto)
    {
        var (userId, userName, _) = await GetCurrentUserInfo();
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();

        contract.ApprovalStatus = ApprovalStatus.Rejected;
        contract.UpdatedAt = DateTime.UtcNow;

        AddAuditLog(contract.Id, userId, userName, "ContractRejected",
            $"驳回合同「{contract.Name}」，原因：{dto.Reason ?? "无"}");

        if (contract.CreatedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.ContractRejected,
                Message = $"您的合同「{contract.Name}」({contract.ContractNumber}) 已被驳回",
                ContractId = contract.Id,
                FromUserId = userId,
                ToUserId = contract.CreatedBy.Value,
                RejectReason = dto.Reason
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "合同已驳回" });
    }

    // ────────── 金额审批 ──────────

    [HttpPost("{id}/submit-amount")]
    public async Task<IActionResult> SubmitAmount(int id, [FromBody] SubmitAmountDto dto)
    {
        var (userId, userName, _) = await GetCurrentUserInfo();
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();
        if (contract.ContractStatus == ContractStatus.Terminated)
            return BadRequest("已终止的合同不可变更金额");

        contract.SubmittedAmount = dto.Amount;
        contract.SubmittedBy = userId;
        contract.UpdatedAt = DateTime.UtcNow;

        AddAuditLog(contract.Id, userId, userName, "AmountSubmitted",
            $"提交金额变更 ¥{dto.Amount:F2}（原 ¥{contract.TotalAmount:F2}）");

        var superAdmins = await _db.Users.Where(u => u.Role == UserRole.SuperAdmin).ToListAsync();
        foreach (var admin in superAdmins)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.AmountApproval,
                Message = $"用户 {userName} 为合同「{contract.Name}」提交了金额 ¥{dto.Amount:F2}，等待审批",
                ContractId = contract.Id,
                FromUserId = userId,
                ToUserId = admin.Id
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "金额已提交审批" });
    }

    [HttpGet("pending-amounts")]
    public async Task<IActionResult> GetPendingAmounts()
    {
        var contracts = await _db.Contracts
            .Where(c => c.SubmittedAmount > 0 && c.SubmittedBy != null)
            .OrderByDescending(c => c.UpdatedAt).ToListAsync();
        return Ok(contracts);
    }

    [HttpPost("{id}/approve-amount")]
    public async Task<IActionResult> ApproveAmount(int id)
    {
        var (userId, userName, _) = await GetCurrentUserInfo();
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();

        var oldAmount = contract.TotalAmount;
        contract.TotalAmount = contract.SubmittedAmount;
        contract.SubmittedAmount = 0;
        var submittedBy = contract.SubmittedBy;
        contract.SubmittedBy = null;
        contract.UpdatedAt = DateTime.UtcNow;

        AddAuditLog(contract.Id, userId, userName, "AmountApproved",
            $"审批通过金额变更：¥{oldAmount:F2} → ¥{contract.TotalAmount:F2}");

        if (submittedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.AmountApproved,
                Message = $"合同「{contract.Name}」的金额变更已审批通过，新金额 ¥{contract.TotalAmount:F2}",
                ContractId = contract.Id,
                FromUserId = userId,
                ToUserId = submittedBy.Value
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "金额审批通过" });
    }

    [HttpPost("{id}/reject-amount")]
    public async Task<IActionResult> RejectAmount(int id, [FromBody] RejectReasonDto dto)
    {
        var (userId, userName, _) = await GetCurrentUserInfo();
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();

        var submittedBy = contract.SubmittedBy;
        contract.SubmittedAmount = 0;
        contract.SubmittedBy = null;
        contract.UpdatedAt = DateTime.UtcNow;

        AddAuditLog(contract.Id, userId, userName, "AmountRejected",
            $"驳回金额变更，原因：{dto.Reason ?? "无"}");

        if (submittedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.AmountRejected,
                Message = $"合同「{contract.Name}」的金额变更已被驳回",
                ContractId = contract.Id,
                FromUserId = userId,
                ToUserId = submittedBy.Value,
                RejectReason = dto.Reason
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "金额已驳回" });
    }

    // ────────── 支付记录 ──────────

    [HttpPost("{id}/payments")]
    public async Task<IActionResult> AddPayment(int id, [FromBody] PaymentCreateDto dto)
    {
        var (userId, userName, isSuperAdmin) = await GetCurrentUserInfo();

        var contract = await _db.Contracts.Include(c => c.Payments).FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (contract.ContractStatus == ContractStatus.Terminated)
            return BadRequest("已终止的合同不可添加支付");

        var approvedPaid = contract.Payments.Where(p => p.Status == PaymentStatus.Approved).Sum(p => p.Amount);
        if (approvedPaid + dto.Amount > contract.TotalAmount)
            return BadRequest("支付金额超过合同总额");

        var payment = new Payment
        {
            ContractId = id,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            Note = dto.Note,
            CreatedBy = userId,
            Status = isSuperAdmin ? PaymentStatus.Approved : PaymentStatus.Pending
        };

        _db.Payments.Add(payment);

        if (isSuperAdmin)
        {
            contract.PaidAmount += dto.Amount;
            AddAuditLog(contract.Id, userId, userName, "PaymentAdded",
                $"添加支付 ¥{dto.Amount:F2}（直接通过）");
        }
        else
        {
            AddAuditLog(contract.Id, userId, userName, "PaymentAdded",
                $"提交支付 ¥{dto.Amount:F2}，等待审批");

            var superAdmins = await _db.Users.Where(u => u.Role == UserRole.SuperAdmin).ToListAsync();
            foreach (var admin in superAdmins)
            {
                _db.Notifications.Add(new Notification
                {
                    Type = NotificationType.PaymentApproval,
                    Message = $"用户 {userName} 为合同「{contract.Name}」提交了支付 ¥{dto.Amount:F2}，等待审批",
                    ContractId = contract.Id,
                    FromUserId = userId,
                    ToUserId = admin.Id
                });
            }
        }

        contract.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(payment);
    }

    [HttpGet("{id}/payments")]
    public async Task<IActionResult> GetPayments(int id)
    {
        var payments = await _db.Payments
            .Where(p => p.ContractId == id)
            .OrderByDescending(p => p.PaymentDate).ToListAsync();
        return Ok(payments);
    }

    [HttpGet("pending-payments")]
    public async Task<IActionResult> GetPendingPayments()
    {
        var payments = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id, p.ContractId, p.Amount, p.PaymentDate, p.Note, p.Status, p.CreatedBy, p.CreatedAt,
                ContractName = p.Contract.Name,
                ContractNumber = p.Contract.ContractNumber
            }).ToListAsync();
        return Ok(payments);
    }

    [HttpPost("payments/{paymentId}/approve")]
    public async Task<IActionResult> ApprovePayment(int paymentId)
    {
        var (userId, userName, _) = await GetCurrentUserInfo();
        var payment = await _db.Payments.Include(p => p.Contract).FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null) return NotFound();
        if (payment.Status != PaymentStatus.Pending) return BadRequest("该支付已处理");

        payment.Status = PaymentStatus.Approved;
        payment.Contract.PaidAmount += payment.Amount;
        payment.Contract.UpdatedAt = DateTime.UtcNow;

        AddAuditLog(payment.ContractId, userId, userName, "PaymentApproved",
            $"审批通过支付 ¥{payment.Amount:F2}");

        if (payment.CreatedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.PaymentApproved,
                Message = $"合同「{payment.Contract.Name}」的支付 ¥{payment.Amount:F2} 已审批通过",
                ContractId = payment.ContractId,
                FromUserId = userId,
                ToUserId = payment.CreatedBy.Value
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "支付审批通过" });
    }

    [HttpPost("payments/{paymentId}/reject")]
    public async Task<IActionResult> RejectPayment(int paymentId, [FromBody] RejectReasonDto dto)
    {
        var (userId, userName, _) = await GetCurrentUserInfo();
        var payment = await _db.Payments.Include(p => p.Contract).FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null) return NotFound();
        if (payment.Status != PaymentStatus.Pending) return BadRequest("该支付已处理");

        payment.Status = PaymentStatus.Rejected;

        AddAuditLog(payment.ContractId, userId, userName, "PaymentRejected",
            $"驳回支付 ¥{payment.Amount:F2}，原因：{dto.Reason ?? "无"}");

        if (payment.CreatedBy.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.PaymentRejected,
                Message = $"合同「{payment.Contract.Name}」的支付 ¥{payment.Amount:F2} 已被驳回",
                ContractId = payment.ContractId,
                FromUserId = userId,
                ToUserId = payment.CreatedBy.Value,
                RejectReason = dto.Reason
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "支付已驳回" });
    }

    // ────────── 文件下载 ──────────

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract is null) return NotFound();
        if (string.IsNullOrEmpty(contract.FilePath)) return NotFound("合同文件不存在");

        var filePath = Path.Combine(_environment.ContentRootPath, contract.FilePath);
        if (!System.IO.File.Exists(filePath)) return NotFound("文件不存在");

        var memory = new MemoryStream();
        await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;
        return File(memory, "application/octet-stream", contract.FileName ?? "contract.bin");
    }

    // ────────── 操作日志 ──────────

    [HttpGet("{id}/logs")]
    public async Task<IActionResult> GetContractLogs(int id)
    {
        var logs = await _db.AuditLogs
            .Where(l => l.ContractId == id)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
        return Ok(logs);
    }
}

// ────────── DTO ──────────

public class ContractCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? ContractNumber { get; set; }
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public IFormFile? File { get; set; }
}

public class ContractUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
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
