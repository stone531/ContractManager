using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using server.Data;
using server.Models;
using server.Services;

var builder = WebApplication.CreateBuilder(args);

// 兼容两种启动方式：
// 1) dotnet run --project server/server.csproj （内容根通常为 server/）
// 2) dotnet server/bin/.../server.dll （内容根通常为仓库根目录）
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("server/appsettings.json", optional: true, reloadOnChange: true);

// 数据库：SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// JWT 认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("JWT Key not configured")))
        };
    });

builder.Services.AddAuthorization();

// 注册服务
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// CORS：允许 Vue 开发服务器访问
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueApp", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 初始化数据库结构与演示数据
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // 若数据库不存在则创建（会根据当前模型建表）
    db.Database.EnsureCreated();

    // 补全已有数据库中缺失的列和表（兼容旧版 app.db）
    EnsureSchema(db);

    SeedInitialData(db);
}

app.UseCors("VueApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static void EnsureSchema(AppDbContext db)
{
    var conn = db.Database.GetDbConnection();
    conn.Open();
    using var cmd = conn.CreateCommand();

    void TryExec(string sql) { try { cmd.CommandText = sql; cmd.ExecuteNonQuery(); } catch { } }

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

static void SeedInitialData(AppDbContext db)
{
    if (!db.Users.Any())
    {
        var hasher = new PasswordHasher<User>();

        // 超管 admin
        var admin = new User { UserName = "admin", Name = "超级管理员", Email = "admin@example.com", Role = UserRole.SuperAdmin, IsEnabled = true, CreatedAt = DateTime.UtcNow };
        admin.PasswordHash = hasher.HashPassword(admin, "admin");

        var u1 = new User { UserName = "zhangsan", Name = "张三", Email = "zhangsan@example.com", Role = UserRole.User, IsEnabled = true, CreatedAt = DateTime.UtcNow };
        u1.PasswordHash = hasher.HashPassword(u1, "password123");

        var u2 = new User { UserName = "lisi", Name = "李四", Email = "lisi@example.com", Role = UserRole.User, IsEnabled = true, CreatedAt = DateTime.UtcNow };
        u2.PasswordHash = hasher.HashPassword(u2, "password123");

        var u3 = new User { UserName = "wangwu", Name = "王五", Email = "wangwu@example.com", Role = UserRole.User, IsEnabled = true, CreatedAt = DateTime.UtcNow };
        u3.PasswordHash = hasher.HashPassword(u3, "password123");

        db.Users.AddRange(admin, u1, u2, u3);
        db.SaveChanges();
    }

    if (!db.Contracts.Any())
    {
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

    if (!db.Payments.Any())
    {
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