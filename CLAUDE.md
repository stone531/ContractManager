# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Projects in this Solution

The solution (`ContractManager.sln`) contains two projects:

- **`server/`** — ASP.NET Core Web API, .NET 8, SQLite via EF Core, JWT auth.
- **`web/`** — Vue 3 SPA with Vite, Pinia, Vue Router, and Axios.

## Commands

### Backend (`server/`)

```bash
# Run the API (listens on http://localhost:5000)
cd server && dotnet run

# Apply migrations / create DB
cd server && dotnet ef database update

# Add a new migration
cd server && dotnet ef migrations add <MigrationName>

# Build
dotnet build ContractManager.sln
```

### Frontend (`web/`)

```bash
# Install deps (first time)
cd web && npm install

# Dev server (http://localhost:5173, proxies /api → http://localhost:5000)
cd web && npm run dev

# Production build
cd web && npm run build
```

## Architecture

### Backend

- **`server/Program.cs`** — Bootstraps DI: SQLite DbContext, JWT Bearer auth, CORS for Vue dev server (`http://localhost:5173`), Swagger, controllers. Also runs `EnsureCreated()` + raw SQL fallback DDL to handle schema gaps, and seeds demo users/contracts on first run.
- **`server/Data/AppDbContext.cs`** — EF Core DbContext backed by `app.db` (SQLite, in the working directory).
- **`server/Models/`** — `Contract`, `Payment`, `User` entities. DTOs under `Models/DTOs/`.
- **`server/Controllers/`** — `AuthController` (`POST /api/auth/register`, `POST /api/auth/login`), `ContractsController` (CRUD + file upload/download), `UsersController`.
- **`server/Services/TokenService.cs`** — Generates JWT tokens; config from `appsettings.json` under `Jwt:Key/Issuer/Audience/ExpirationMinutes`.
- **`server/uploads/`** — Contract file attachments stored here at runtime.

### Frontend

- **`web/src/api/axios.js`** — Axios instance with `Authorization: Bearer <token>` interceptor.
- **`web/src/stores/auth.js`** — Pinia store managing login state and JWT token (persisted to localStorage).
- **`web/src/router/index.js`** — Route definitions; guard redirects unauthenticated users to `/login`.
- **`web/src/layouts/MainLayout.vue`** — Shell layout wrapping all authenticated views.
- **`web/src/views/`** — `LoginView`, `RegisterView`, `DashboardView`, `ContractListView`, `ContractAddView`, `ContractEditView`, `ContractDetailView`, `UserListView`, `UserAddView`, `UserEditView`.

### Key Domain Concepts

- **Contract** — has `TotalAmount`, `OriginalAmount` (for change tracking/comparison), `PaidAmount`, optional file attachment, and a collection of `Payment` records.
- **Payment** — belongs to a Contract; records amount, date, and note. Adding a payment updates `Contract.PaidAmount`.
- The frontend highlights amount changes (green = increase, red = decrease) by comparing `TotalAmount` vs `OriginalAmount`.

### Auth Flow

Register/login → backend returns `{ token, user }` → Pinia auth store persists token → Axios attaches `Authorization: Bearer <token>` header → protected API routes require `[Authorize]`.

### Vite Proxy

All `/api/*` requests from the frontend dev server are proxied to `http://localhost:5000`. In production, configure a reverse proxy (nginx/IIS) instead.

### Demo Seed Accounts

On first run, three accounts are seeded: `zhangsan@example.com`, `lisi@example.com`, `wangwu@example.com` — all with password `password123`.
