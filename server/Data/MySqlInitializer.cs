using Microsoft.EntityFrameworkCore;
using server.Models;

namespace server.Data;

/// <summary>
/// MySQL 数据库初始化器
/// </summary>
public class MySqlInitializer : IDatabaseInitializer
{
    public void EnsureSchema(AppDbContext db)
    {
        // EnsureCreated 已在 Program.cs 中调用，此处仅补全可能缺失的列（兼容旧版数据库升级）
        var conn = db.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();

        void TryExec(string sql)
        {
            try { cmd.CommandText = sql; cmd.ExecuteNonQuery(); } catch { }
        }

        // Contracts 表补列
        TryExec("ALTER TABLE Contracts ADD COLUMN ContractNumber VARCHAR(100) NULL");
        TryExec("ALTER TABLE Contracts ADD COLUMN ApprovalStatus INT NOT NULL DEFAULT 0");
        TryExec("ALTER TABLE Contracts ADD COLUMN ContractStatus INT NOT NULL DEFAULT 0");
        TryExec("ALTER TABLE Contracts ADD COLUMN SubmittedAmount DECIMAL(18,2) NOT NULL DEFAULT 0");
        TryExec("ALTER TABLE Contracts ADD COLUMN SubmittedBy INT NULL");
        TryExec("ALTER TABLE Contracts ADD COLUMN ApprovedAt DATETIME(6) NULL");
        TryExec("ALTER TABLE Contracts ADD COLUMN CreatedBy INT NULL");
        TryExec("ALTER TABLE Contracts ADD COLUMN StartDate DATETIME(6) NULL");
        TryExec("ALTER TABLE Contracts ADD COLUMN EndDate DATETIME(6) NULL");
        TryExec("ALTER TABLE Contracts ADD COLUMN TerminatedAt DATETIME(6) NULL");
        TryExec("ALTER TABLE Contracts ADD COLUMN TerminatedBy INT NULL");

        // Payments 表补列
        TryExec("ALTER TABLE Payments ADD COLUMN Status INT NOT NULL DEFAULT 0");
        TryExec("ALTER TABLE Payments ADD COLUMN CreatedBy INT NULL");
        TryExec("ALTER TABLE Payments ADD COLUMN CreatedAt DATETIME(6) NOT NULL DEFAULT '2024-01-01 00:00:00'");

        // Users 表补列
        TryExec("ALTER TABLE Users ADD COLUMN UserName VARCHAR(100) NOT NULL DEFAULT ''");
        TryExec("ALTER TABLE Users ADD COLUMN Role INT NOT NULL DEFAULT 2");
        TryExec("ALTER TABLE Users ADD COLUMN IsEnabled TINYINT(1) NOT NULL DEFAULT 1");

        conn.Close();
    }

    public void SeedData(AppDbContext db)
    {
        DatabaseSeeder.Seed(db);
    }
}
