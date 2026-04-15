using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server.Models;

namespace server.Data;

/// <summary>
/// SQLite 数据库初始化器
/// </summary>
public class SqliteInitializer : IDatabaseInitializer
{
    public void EnsureSchema(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();

        void TryExec(string sql)
        {
            try { cmd.CommandText = sql; cmd.ExecuteNonQuery(); } catch { }
        }

        // Contracts 表补列
        TryExec("ALTER TABLE Contracts ADD COLUMN ContractNumber TEXT");
        TryExec("ALTER TABLE Contracts ADD COLUMN ApprovalStatus INTEGER NOT NULL DEFAULT 0");
        TryExec("ALTER TABLE Contracts ADD COLUMN ContractStatus INTEGER NOT NULL DEFAULT 0");
        TryExec("ALTER TABLE Contracts ADD COLUMN SubmittedAmount REAL NOT NULL DEFAULT 0");
        TryExec("ALTER TABLE Contracts ADD COLUMN SubmittedBy INTEGER");
        TryExec("ALTER TABLE Contracts ADD COLUMN ApprovedAt TEXT");
        TryExec("ALTER TABLE Contracts ADD COLUMN CreatedBy INTEGER");
        TryExec("ALTER TABLE Contracts ADD COLUMN StartDate TEXT");
        TryExec("ALTER TABLE Contracts ADD COLUMN EndDate TEXT");
        TryExec("ALTER TABLE Contracts ADD COLUMN TerminatedAt TEXT");
        TryExec("ALTER TABLE Contracts ADD COLUMN TerminatedBy INTEGER");

        // Payments 表补列
        TryExec("ALTER TABLE Payments ADD COLUMN Status INTEGER NOT NULL DEFAULT 0");
        TryExec("ALTER TABLE Payments ADD COLUMN CreatedBy INTEGER");
        TryExec("ALTER TABLE Payments ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT '2024-01-01T00:00:00'");

        // Users 表补列
        TryExec("ALTER TABLE Users ADD COLUMN UserName TEXT NOT NULL DEFAULT ''");
        TryExec("ALTER TABLE Users ADD COLUMN Role INTEGER NOT NULL DEFAULT 2");
        TryExec("ALTER TABLE Users ADD COLUMN IsEnabled INTEGER NOT NULL DEFAULT 1");

        // Notifications 表（新表）
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Notifications (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Type INTEGER NOT NULL,
            Message TEXT NOT NULL,
            ContractId INTEGER,
            FromUserId INTEGER NOT NULL DEFAULT 0,
            ToUserId INTEGER NOT NULL DEFAULT 0,
            IsRead INTEGER NOT NULL DEFAULT 0,
            RejectReason TEXT,
            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
        )";
        cmd.ExecuteNonQuery();

        // AuditLogs 表（新表）
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS AuditLogs (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ContractId INTEGER,
            UserId INTEGER NOT NULL DEFAULT 0,
            UserName TEXT NOT NULL DEFAULT '',
            Action TEXT NOT NULL DEFAULT '',
            Description TEXT NOT NULL DEFAULT '',
            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
        )";
        cmd.ExecuteNonQuery();

        conn.Close();
    }

    public void SeedData(AppDbContext db)
    {
        DatabaseSeeder.Seed(db);
    }
}
