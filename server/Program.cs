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
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 初始化数据库结构与演示数据
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // 若数据库不存在则创建（会根据当前模型建表）
    db.Database.EnsureCreated();

    SeedInitialData(db);
}

app.UseCors("VueApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

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
        var c1 = new Contract
        {
            Name = "办公设备采购合同",
            Description = "用于公司办公设备采购",
            TotalAmount = 120000,
            OriginalAmount = 120000,
            PaidAmount = 20000,
            CreatedAt = DateTime.UtcNow
        };

        var c2 = new Contract
        {
            Name = "软件开发服务合同",
            Description = "外包开发项目",
            TotalAmount = 300000,
            OriginalAmount = 300000,
            PaidAmount = 0,
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
                CreatedAt = DateTime.UtcNow
            });

            firstContract.PaidAmount = Math.Max(firstContract.PaidAmount, 20000);
            db.SaveChanges();
        }
    }
}