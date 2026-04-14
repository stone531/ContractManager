# Contract Manager - 合同管理系统

一个功能完整的合同管理系统，支持合同创建、编辑、支付记录管理和文件上传下载。

## 🚀 技术栈

### 后端 (server/)
- **.NET 8.0** - Web API框架
- **Entity Framework Core** - ORM
- **SQLite** - 数据库
- **JWT Authentication** - 身份认证
- **Swagger** - API文档

### 前端 (web/)
- **Vue 3** - 前端框架
- **Vue Router** - 路由管理
- **Pinia** - 状态管理
- **Axios** - HTTP客户端
- **Vite** - 构建工具

## 📋 功能特性

- ✅ 用户注册和登录（JWT认证）
- ✅ 合同增删改查
- ✅ 合同文件上传和下载
- ✅ 支付记录管理
- ✅ 合同金额智能对比（绿色=增加，红色=减少）
- ✅ 支付进度可视化
- ✅ 响应式设计，支持移动端

## 💻 环境要求

- **.NET SDK 8.0** 或更高版本
- **Node.js 16+** 和 npm
- Git

## 🔧 安装步骤

### 1. 克隆仓库
```bash
git clone https://github.com/stone531/ContractManager.git
cd ContractManager
```

### 2. 后端设置
```bash
cd server

# 恢复依赖包
dotnet restore

# 创建数据库
dotnet ef database update

# 或者添加迁移后再更新（如果需要）
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. 前端设置
```bash
cd ../web

# 安装依赖
npm install
```

## ▶️ 运行项目

### 启动后端服务器
```bash
cd server
dotnet run
```
后端将运行在: `http://localhost:5000`

### 启动前端开发服务器
```bash
cd web
npm run dev
```
前端将运行在: `http://localhost:5173`

## 🗄️ 数据库迁移

如果修改了模型，需要创建新的迁移：

```bash
cd server

# 添加迁移
dotnet ef migrations add <MigrationName>

# 应用迁移
dotnet ef database update

# 回滚迁移（可选）
dotnet ef database update <PreviousMigrationName>

# 删除最后一个迁移（可选）
dotnet ef migrations remove
```

## 📝 编译发布

### 后端编译
```bash
cd server
dotnet publish -c Release -o ../publish/server
```

### 前端编译
```bash
cd web
npm run build
```
编译后的文件在 `web/dist/` 目录

## 🌐 API文档

后端运行后，访问 Swagger UI:
```
http://localhost:5000/swagger
```

## 📂 项目结构

```
ContractManager/
├── server/                 # 后端 (.NET)
│   ├── Controllers/        # API控制器
│   ├── Models/            # 数据模型
│   ├── Data/              # 数据库上下文
│   ├── Services/          # 业务服务
│   ├── Migrations/        # 数据库迁移
│   └── server.csproj      # 项目文件
│
├── web/                   # 前端 (Vue 3)
│   ├── src/
│   │   ├── api/          # API接口
│   │   ├── views/        # 页面组件
│   │   ├── layouts/      # 布局组件
│   │   ├── stores/       # 状态管理
│   │   └── router/       # 路由配置
│   ├── package.json      # 依赖配置
│   └── vite.config.js    # Vite配置
│
├── .gitignore            # Git忽略文件
└── README.md             # 项目说明
```

## 🔐 默认账号

系统默认无账号，请先注册新用户。

## 📸 主要功能截图

- 合同列表：支持查看、编辑、下载
- 合同编辑：金额对比提示（绿色/红色）
- 支付记录：可视化进度条
- 合同详情：完整的支付历史

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License

## 👨‍💻 作者

stone531

## 🐛 问题反馈

如有问题，请在 [Issues](https://github.com/stone531/ContractManager/issues) 中反馈。
