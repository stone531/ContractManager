using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using server.Models;

namespace server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 配置 User Email 唯一索引
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // 配置 Contract �?Payment 的关�?        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Contract)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置 Decimal 精度
        modelBuilder.Entity<Contract>()
            .Property(c => c.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Contract>()
            .Property(c => c.PaidAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        // 种子数据 - 使用预生成的密码哈希
        // 密码 "password123" 的哈希值（使用 PasswordHasher<User> 生成�?        var hasher = new PasswordHasher<User>();
        var tempUser = new User();
        var passwordHash = hasher.HashPassword(tempUser, "password123");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Name = "张三",
                Email = "zhangsan@example.com",
                PasswordHash = passwordHash,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 2,
                Name = "李四",
                Email = "lisi@example.com",
                PasswordHash = passwordHash,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 3,
                Name = "王五",
                Email = "wangwu@example.com",
                PasswordHash = passwordHash,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
