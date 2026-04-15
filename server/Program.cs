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

// ────────── 数据库配置 ──────────
var dbType = builder.Configuration.GetValue<int>("Database:DbType");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (dbType)
    {
        case 2: // MySQL
            var host = builder.Configuration["Database:MySQL:DbHost"] ?? "localhost";
            var port = builder.Configuration.GetValue<int>("Database:MySQL:DbPort", 3306);
            var dbName = builder.Configuration["Database:MySQL:DbName"] ?? "ContractManager";
            var user = builder.Configuration["Database:MySQL:DbUser"] ?? "root";
            var password = builder.Configuration["Database:MySQL:DbPassword"] ?? "";
            var mysqlConnStr = $"Server={host};Port={port};Database={dbName};User={user};Password={password};CharSet=utf8mb4;";
            ServerVersion serverVersion;
            try
            {
                serverVersion = ServerVersion.AutoDetect(mysqlConnStr);
            }
            catch
            {
                // 无法自动探测时回退到 MySQL 8.0
                serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
            }
            options.UseMySql(mysqlConnStr, serverVersion);
            break;

        case 1: // SQLite（默认）
        default:
            var dbPath = builder.Configuration["Database:SQLite:DbPath"] ?? "app.db";
            options.UseSqlite($"Data Source={dbPath}");
            break;
    }
});

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

// ────────── 初始化数据库 ──────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // 若数据库不存在则创建（会根据当前模型建表）
    db.Database.EnsureCreated();

    // 根据数据库类型选择初始化器
    var initializer = DatabaseInitializerFactory.Create(dbType);
    initializer.EnsureSchema(db);
    initializer.SeedData(db);

    var dbTypeName = dbType == 2 ? "MySQL" : "SQLite";
    app.Logger.LogInformation("数据库初始化完成，使用 {DbType} 数据库", dbTypeName);
}

app.UseCors("VueApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
