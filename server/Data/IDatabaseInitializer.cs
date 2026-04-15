namespace server.Data;

/// <summary>
/// 数据库初始化器接口，用于兼容不同数据库的 Schema 补全与种子数据
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// 确保数据库 Schema 完整（补全缺失的列和表）
    /// </summary>
    void EnsureSchema(AppDbContext db);

    /// <summary>
    /// 插入初始种子数据（用户、示例合同等）
    /// </summary>
    void SeedData(AppDbContext db);
}
