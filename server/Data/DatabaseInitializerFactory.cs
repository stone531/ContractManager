namespace server.Data;

/// <summary>
/// 数据库初始化器工厂，根据 DbType 配置选择对应的初始化器
/// DbType = 1: SQLite
/// DbType = 2: MySQL
/// </summary>
public static class DatabaseInitializerFactory
{
    public static IDatabaseInitializer Create(int dbType)
    {
        return dbType switch
        {
            1 => new SqliteInitializer(),
            2 => new MySqlInitializer(),
            _ => throw new ArgumentException($"不支持的数据库类型: {dbType}。请在 appsettings.json 中设置 Database:DbType 为 1(SQLite) 或 2(MySQL)")
        };
    }
}
