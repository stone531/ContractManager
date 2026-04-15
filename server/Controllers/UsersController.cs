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
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.Name.Contains(search) ||
                u.Email.Contains(search) ||
                u.UserName.Contains(search));
        }

        var total = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { items = users, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("用户名和密码不能为空");

        if (dto.Password.Length < 6)
            return BadRequest("密码至少需要 6 个字符");

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        // 防止创建 SuperAdmin
        if (dto.Role == UserRole.SuperAdmin)
            return BadRequest(new { message = "不允许创建超级管理员" });

        var user = new User
        {
            UserName = dto.UserName,
            Name = dto.Name,
            Email = dto.Email,
            Role = dto.Role,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    public class CreateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] User user)
    {
        if (id != user.Id) return BadRequest("用户 ID 不匹配");

        var existingUser = await _db.Users.FindAsync(id);
        if (existingUser is null) return NotFound();

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;

        await _db.SaveChangesAsync();
        return Ok(existingUser);
    }

    // ────────── 管理员重置用户密码 ──────────
    [HttpPut("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
    {
        // 仅 SuperAdmin 可操作
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            return Unauthorized();

        var currentUser = await _db.Users.FindAsync(currentUserId);
        if (currentUser == null || currentUser.Role != UserRole.SuperAdmin)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest(new { message = "密码至少需要 6 个字符" });

        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "密码重置成功" });
    }

    public class ResetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    // ────────── 用户修改自己密码 ──────────
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            return Unauthorized();

        var user = await _db.Users.FindAsync(currentUserId);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest(new { message = "新密码至少需要 6 个字符" });

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.OldPassword);
        if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            return BadRequest(new { message = "旧密码不正确" });

        user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "密码修改成功" });
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // 仅管理员可删除用户
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            return Unauthorized();

        var currentUser = await _db.Users.FindAsync(currentUserId);
        if (currentUser == null || currentUser.Role != UserRole.SuperAdmin)
            return Forbid();

        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        // SuperAdmin 不能删除自己
        if (currentUserId == id)
            return BadRequest(new { message = "超级管理员无法删除自己" });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}