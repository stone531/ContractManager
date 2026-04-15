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

    // 获取当前用户的通知列表
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var notifications = await _db.Notifications
            .Where(n => n.ToUserId == userId.Value)
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
