using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models;
using server.Models.DTOs;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(AppDbContext db, TokenService tokenService, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "姓名、邮箱和密码不能为空" });
        }

        // 验证密码长度
        if (dto.Password.Length < 6)
        {
            return BadRequest(new { message = "密码至少需�?个字�? });
        }

        // 检查邮箱是否已存在
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "该邮箱已被注�? });
        }

        // 创建新用�?        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            CreatedAt = DateTime.UtcNow
        };

        // 哈希密码
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        // 保存到数据库
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // 生成 JWT 令牌
        var token = _tokenService.GenerateToken(user);

        // 返回令牌和用户信�?        var userDto = new UserDto(user.Id, user.Name, user.Email, user.CreatedAt);
        var response = new AuthResponseDto(token, userDto);

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "邮箱和密码不能为�? });
        }

        // 查找用户
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "邮箱或密码错�? });
        }

        // 验证密码
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "邮箱或密码错�? });
        }

        // 生成 JWT 令牌
        var token = _tokenService.GenerateToken(user);

        // 返回令牌和用户信�?        var userDto = new UserDto(user.Id, user.Name, user.Email, user.CreatedAt);
        var response = new AuthResponseDto(token, userDto);

        return Ok(response);
    }
}
