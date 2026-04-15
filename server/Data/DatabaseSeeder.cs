using Microsoft.AspNetCore.Identity;
using server.Models;

namespace server.Data;

/// <summary>
/// 共享的种子数据逻辑（SQLite 和 MySQL 通用）
/// </summary>
public static class DatabaseSeeder
{
    public static void Seed(AppDbContext db)
    {
        SeedUsers(db);
        SeedContracts(db);
        SeedPayments(db);
    }

    private static void SeedUsers(AppDbContext db)
    {
        if (db.Users.Any()) return;

        var hasher = new PasswordHasher<User>();

        // 超管 admin
        var admin = new User
        {
            UserName = "admin",
            Name = "超级管理员",
            Email = "admin@example.com",
            Role = UserRole.SuperAdmin,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, "admin");

        // 普通用户 test
        var test = new User
        {
            UserName = "test",
            Name = "测试用户",
            Email = "test@example.com",
            Role = UserRole.User,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };
        test.PasswordHash = hasher.HashPassword(test, "test123");

        db.Users.AddRange(admin, test);
        db.SaveChanges();
    }

    private static void SeedContracts(AppDbContext db)
    {
        if (db.Contracts.Any()) return;

        var adminUser = db.Users.FirstOrDefault(u => u.Role == UserRole.SuperAdmin);
        var adminId = adminUser?.Id;

        var c1 = new Contract
        {
            ContractNumber = "HT260413001",
            Name = "办公设备采购合同",
            Description = "用于公司办公设备采购",
            TotalAmount = 120000,
            OriginalAmount = 120000,
            PaidAmount = 20000,
            CreatedBy = adminId,
            ApprovalStatus = ApprovalStatus.Approved,
            ContractStatus = ContractStatus.InProgress,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(335),
            CreatedAt = DateTime.UtcNow
        };

        var c2 = new Contract
        {
            ContractNumber = "HT260413002",
            Name = "软件开发服务合同",
            Description = "外包开发项目",
            TotalAmount = 300000,
            OriginalAmount = 300000,
            PaidAmount = 0,
            CreatedBy = adminId,
            ApprovalStatus = ApprovalStatus.Approved,
            ContractStatus = ContractStatus.InProgress,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(170),
            CreatedAt = DateTime.UtcNow
        };

        db.Contracts.AddRange(c1, c2);
        db.SaveChanges();
    }

    private static void SeedPayments(AppDbContext db)
    {
        if (db.Payments.Any()) return;

        var firstContract = db.Contracts.OrderBy(c => c.Id).FirstOrDefault();
        if (firstContract != null)
        {
            db.Payments.Add(new Payment
            {
                ContractId = firstContract.Id,
                Amount = 20000,
                PaymentDate = DateTime.UtcNow,
                Note = "首付款",
                Status = PaymentStatus.Approved,
                CreatedAt = DateTime.UtcNow
            });

            firstContract.PaidAmount = Math.Max(firstContract.PaidAmount, 20000);
            db.SaveChanges();
        }
    }
}
