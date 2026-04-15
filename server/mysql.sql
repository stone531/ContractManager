-- ============================================================
-- ContractManager MySQL 初始化脚本（备用方案 B）
-- 使用方法: mysql -u root -p < mysql.sql
-- ============================================================

-- 创建数据库（如不存在）
CREATE DATABASE IF NOT EXISTS ContractManager
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_unicode_ci;

USE ContractManager;

-- ============================================================
-- 第一部分：建表（可独立执行）
-- ============================================================

-- ────────── 用户表 ──────────
CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserName VARCHAR(100) NOT NULL DEFAULT '',
    Name VARCHAR(200) NOT NULL,
    Email VARCHAR(200) NOT NULL,
    PasswordHash TEXT NOT NULL,
    Role INT NOT NULL DEFAULT 2 COMMENT '0=SuperAdmin, 1=Admin, 2=User',
    IsEnabled TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UNIQUE INDEX IX_Users_Email (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ────────── 合同表 ──────────
CREATE TABLE IF NOT EXISTS Contracts (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ContractNumber VARCHAR(100) NULL,
    Name VARCHAR(500) NOT NULL,
    Description TEXT NULL,
    FileName VARCHAR(500) NULL,
    FilePath VARCHAR(1000) NULL,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    OriginalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL,
    ApprovalStatus INT NOT NULL DEFAULT 0 COMMENT '0=Pending, 1=Approved, 2=Rejected',
    ContractStatus INT NOT NULL DEFAULT 0 COMMENT '0=Initial, 1=InProgress, 2=Completed, 3=Terminated',
    SubmittedAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    SubmittedBy INT NULL,
    ApprovedAt DATETIME(6) NULL,
    CreatedBy INT NULL,
    StartDate DATETIME(6) NULL,
    EndDate DATETIME(6) NULL,
    TerminatedAt DATETIME(6) NULL,
    TerminatedBy INT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ────────── 支付记录表 ──────────
CREATE TABLE IF NOT EXISTS Payments (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ContractId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaymentDate DATETIME(6) NOT NULL,
    Note TEXT NULL,
    Status INT NOT NULL DEFAULT 0 COMMENT '0=Pending, 1=Approved, 2=Rejected',
    CreatedBy INT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT FK_Payments_Contracts FOREIGN KEY (ContractId) REFERENCES Contracts(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ────────── 通知表 ──────────
CREATE TABLE IF NOT EXISTS Notifications (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Type INT NOT NULL COMMENT '通知类型枚举',
    Message TEXT NOT NULL,
    ContractId INT NULL,
    FromUserId INT NOT NULL DEFAULT 0,
    ToUserId INT NOT NULL DEFAULT 0,
    IsRead TINYINT(1) NOT NULL DEFAULT 0,
    RejectReason TEXT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ────────── 审计日志表 ──────────
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ContractId INT NULL,
    UserId INT NOT NULL DEFAULT 0,
    UserName VARCHAR(200) NOT NULL DEFAULT '',
    Action VARCHAR(200) NOT NULL DEFAULT '',
    Description TEXT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 第二部分：种子数据（可选执行）
-- 
-- 注意：以下密码哈希为占位符，导入后无法直接登录。
-- 推荐做法：
--   1. 仅执行上方建表语句
--   2. 启动应用程序，程序会自动创建用户并生成正确的密码哈希
--      默认用户: admin/admin (超级管理员), test/test123 (普通用户)
--
-- 如果你确实需要通过 SQL 插入用户，请在插入后通过程序的
-- 注册接口或修改密码接口来重置密码。
-- ============================================================

-- INSERT INTO Users (UserName, Name, Email, PasswordHash, Role, IsEnabled, CreatedAt) VALUES
-- ('admin', '超级管理员', 'admin@example.com',
--  'PLACEHOLDER_HASH_RESTART_APP_TO_REGENERATE',
--  0, 1, UTC_TIMESTAMP()),
-- ('test', '测试用户', 'test@example.com',
--  'PLACEHOLDER_HASH_RESTART_APP_TO_REGENERATE',
--  2, 1, UTC_TIMESTAMP());
