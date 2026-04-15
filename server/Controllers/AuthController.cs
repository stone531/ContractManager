using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models;
using server.Models.DTOs;
using server.Services;
using System.Security.Claims;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(
        AppDbContext db,
        TokenService tokenService,
        IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName)
            || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "用户名和密码不能为空" });
        }

        if (dto.Password.Length < 6)
        {
            return BadRequest(new { message = "密码至少需要 6 个字符" });
        }

        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == dto.UserName);
        if (existingUser != null)
        {
            return BadRequest(new { message = "该用户名已被注册" });
        }

        var user = new User
        {
            UserName = dto.UserName,
            Name = dto.Name,
            Email = dto.Email,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        var userDto = new UserDto(user.Id, user.Name, user.Email, user.Role, user.CreatedAt);
        var response = new AuthResponseDto(token, userDto);

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "用户名和密码不能为空" });
        }
        
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == dto.UserName);
        if (user == null)
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        if (!user.IsEnabled)
        {
            return Unauthorized(new { message = "账户已被禁用，请联系管理员" });
        }

        var token = _tokenService.GenerateToken(user);
        var userDto = new UserDto(user.Id, user.Name, user.Email, user.Role, user.CreatedAt);
        var response = new AuthResponseDto(token, userDto);

        return Ok(response);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            return BadRequest(new { message = "旧密码和新密码不能为空" });
        }

        if (dto.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "新密码至少需要 6 个字符" });
        }

        // 获取当前用户 ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "用户身份无效" });
        }

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        // 验证旧密码
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.OldPassword);
        if (result == PasswordVerificationResult.Failed)
        {
            return BadRequest(new { message = "旧密码错误" });
        }

        // 设置新密码
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "密码修改成功" });
    }
}

public class ChangePasswordDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
