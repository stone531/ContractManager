using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace server.Models;

public enum UserRole
{
    SuperAdmin = 0,  // 超级管理员
    Admin = 1,       // 管理员
    User = 2         // 普通用户
}

[Index(nameof(UserName), IsUnique = true)]
public class User
{
    public int Id { get; set; }
    
    [StringLength(50)]
    public string UserName { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    // 用户角色
    public UserRole Role { get; set; } = UserRole.User;
    
    // 账户是否禁用
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
