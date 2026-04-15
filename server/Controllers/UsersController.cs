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
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users.ToListAsync();
        return Ok(users);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        // 获取当前用户ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var currentUserId))
        {
            // SuperAdmin 不能删除自己
            if (currentUserId == id && user.Role == UserRole.SuperAdmin)
            {
                return BadRequest(new { message = "超级管理员无法删除自己" });
            }
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}