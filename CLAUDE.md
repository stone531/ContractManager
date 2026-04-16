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

- **`server/Program.cs`** — Bootstraps DI: DbContext (SQLite or MySQL), JWT Bearer auth, CORS for Vue dev server (`http://localhost:5173`), Swagger, controllers. On startup runs `EnsureCreated()` then delegates to `DatabaseInitializerFactory` → `SqliteInitializer` or `MySqlInitializer` to ensure schema gaps, then `DatabaseSeeder` to seed demo data on first run.
- **`server/Data/AppDbContext.cs`** — EF Core DbContext. Contains `DbSet`s for `Contracts`, `Payments`, `Users`, `Notifications`, `AuditLogs`.
- **`server/Data/DatabaseInitializerFactory.cs`** — Selects `SqliteInitializer` or `MySqlInitializer` based on `Database:DbType` config (`1` = SQLite default, `2` = MySQL). Each initializer ensures missing columns exist and calls `DatabaseSeeder.Seed(db)`.
- **`server/Models/`** — Core entities and enums (see Domain Concepts below). DTOs under `Models/DTOs/`.
- **`server/Controllers/`** — `AuthController`, `ContractsController`, `UsersController`, `NotificationsController`.
- **`server/Services/TokenService.cs`** — Generates JWT tokens; config from `appsettings.json` under `Jwt:Key/Issuer/Audience/ExpirationMinutes`.
- **`server/uploads/`** — Contract file attachments stored here at runtime.

### Frontend

- **`web/src/api/axios.js`** — Axios instance with `Authorization: Bearer <token>` interceptor.
- **`web/src/stores/auth.js`** — Pinia store managing login state and JWT token (persisted to localStorage).
- **`web/src/router/index.js`** — Route definitions; guard redirects unauthenticated users to `/login`. Routes under `/approval/*` are restricted to `SuperAdmin` role (checked via `meta.requiresSuperAdmin`).
- **`web/src/layouts/MainLayout.vue`** — Shell layout wrapping all authenticated views.

### Key Domain Concepts

**Contract** — Central entity with two-stage approval workflow:
- `ApprovalStatus` (enum): `Pending / Approved / Rejected` — tracks contract-level approval by SuperAdmin.
- `ContractStatus` (enum): `Initial / InProgress / Completed / Terminated`.
- `TotalAmount` vs `OriginalAmount` — frontend highlights changes (green = increase, red = decrease).
- `SubmittedAmount` + `SubmittedBy` — holds a pending amount change awaiting SuperAdmin approval.
- `PaidAmount` — sum of all `Approved` payments; `RemainingAmount` and `IsFullyPaid` are computed properties.

**Payment** — Belongs to a Contract; has its own `PaymentStatus` (`Pending / Approved / Rejected`). Payments only affect `Contract.PaidAmount` once approved by SuperAdmin.

**User** — Three roles: `SuperAdmin (0)`, `Admin (1)`, `User (2)`. `IsEnabled` flag can disable accounts. Login is by `UserName` (unique), not email.

**Notification** — Point-to-point messages between users (`FromUserId` → `ToUserId`). Nine `NotificationType` values covering contract approval, amount approval, and payment approval (request + approved + rejected variants). The frontend splits these into two categories: `contract` (types 0,2,3) and `amount` (types 1,4,5,6,7,8).

**AuditLog** — Append-only log of actions (create/edit/approve/reject/terminate), written by `ContractsController` helper `AddAuditLog(...)`.

### Approval Workflow

The system has three distinct approval flows, all gated on `SuperAdmin`:

1. **Contract approval** — New contracts start as `ApprovalStatus.Pending`. SuperAdmin approves/rejects via `POST /api/contracts/{id}/approve` or `reject`. Sends `ContractApproved/Rejected` notification to creator.

2. **Amount change approval** — A user submits a new amount (`POST /api/contracts/{id}/submit-amount`), stored in `SubmittedAmount`. SuperAdmin approves via `POST /api/contracts/{id}/approve-amount`, which updates `TotalAmount`. Sends `AmountApproved/Rejected` notification.

3. **Payment approval** — A user adds a payment (`POST /api/contracts/{id}/payments`), which starts as `PaymentStatus.Pending`. SuperAdmin approves via `POST /api/contracts/{id}/payments/{pid}/approve`. Only approved payments count toward `PaidAmount`. Sends `PaymentApproved/Rejected` notification.

Frontend approval views: `ContractApprovalView`, `AmountApprovalView`, `PaymentApprovalView` (all at `/approval/*`, SuperAdmin only).

### Notification API

`GET /api/notifications` — current user's notifications, optional `?category=contract|amount` filter.  
`GET /api/notifications/unread-count` — total unread count.  
`GET /api/notifications/unread-count-by-category` — returns `{ contract, amount }` counts.  
`POST /api/notifications/{id}/read` and `POST /api/notifications/read-all`.

### Auth Flow

Register/login → backend returns `{ token, user }` → Pinia auth store persists token → Axios attaches `Authorization: Bearer <token>` → protected routes require `[Authorize]`. Role is embedded in the JWT and read from `authStore.user.role` (numeric: 0 = SuperAdmin).

### Vite Proxy

All `/api/*` requests from the frontend dev server are proxied to `http://localhost:5000`. In production, configure a reverse proxy (nginx/IIS) instead.

### Demo Seed Accounts

On first run, two accounts are seeded:
- `admin` / `admin` — SuperAdmin
- `test` / `test123` — User

### Database Configuration

Configured in `server/appsettings.json` under `Database:DbType`: `1` = SQLite (default, `app.db` in working directory), `2` = MySQL (connection params under `Database:MySQL:*`).
