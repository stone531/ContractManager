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
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }

    // 获取当前用户的通知列表，支持 category 过滤: contract / amount
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] string? category)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var query = _db.Notifications
            .Where(n => n.ToUserId == userId.Value);

        if (category == "contract")
        {
            // 合同相关通知: 0,2,3
            query = query.Where(n => n.Type == NotificationType.ContractApproval
                || n.Type == NotificationType.ContractApproved
                || n.Type == NotificationType.ContractRejected);
        }
        else if (category == "amount")
        {
            // 金额+支付相关通知: 1,4,5,6,7,8
            query = query.Where(n => n.Type == NotificationType.AmountApproval
                || n.Type == NotificationType.AmountApproved
                || n.Type == NotificationType.AmountRejected
                || n.Type == NotificationType.PaymentApproval
                || n.Type == NotificationType.PaymentApproved
                || n.Type == NotificationType.PaymentRejected);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(notifications);
    }

    // 获取未读通知数量
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var count = await _db.Notifications
            .CountAsync(n => n.ToUserId == userId.Value && !n.IsRead);

        return Ok(new { count });
    }

    // 获取按分类的未读通知数量
    [HttpGet("unread-count-by-category")]
    public async Task<IActionResult> GetUnreadCountByCategory()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var unread = await _db.Notifications
            .Where(n => n.ToUserId == userId.Value && !n.IsRead)
            .ToListAsync();

        var contract = unread.Count(n =>
            n.Type == NotificationType.ContractApproval ||
            n.Type == NotificationType.ContractApproved ||
            n.Type == NotificationType.ContractRejected);

        var amount = unread.Count(n =>
            n.Type == NotificationType.AmountApproval ||
            n.Type == NotificationType.AmountApproved ||
            n.Type == NotificationType.AmountRejected ||
            n.Type == NotificationType.PaymentApproval ||
            n.Type == NotificationType.PaymentApproved ||
            n.Type == NotificationType.PaymentRejected);

        return Ok(new { contract, amount });
    }

    // 标记为已读
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var notification = await _db.Notifications.FindAsync(id);
        if (notification == null) return NotFound();
        if (notification.ToUserId != userId.Value) return Forbid();

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }

    // 全部标记为已读
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var unread = await _db.Notifications
            .Where(n => n.ToUserId == userId.Value && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var id)) return id;
        return null;
    }
}
