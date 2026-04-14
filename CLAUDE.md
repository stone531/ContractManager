# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Projects in this Solution

The solution (`HelloWeb.sln`) contains two projects:

- **`HelloWeb/`** — Minimal ASP.NET Core app (root-level `Program.cs`), serves a static HTML page. Largely a scaffold/prototype.
- **`backend/`** — The real API server: ASP.NET Core Web API, .NET 8, SQLite via EF Core, JWT auth.
- **`frontend/`** — Vue 3 SPA with Vite, Pinia, Vue Router, and Axios.

## Commands

### Backend (`backend/`)

```bash
# Run the API (listens on http://localhost:5000)
cd backend && dotnet run

# Apply migrations / create DB
cd backend && dotnet ef database update

# Add a new migration
cd backend && dotnet ef migrations add <MigrationName>

# Build
dotnet build HelloWeb.sln
```

### Frontend (`frontend/`)

```bash
# Install deps (first time)
cd frontend && npm install

# Dev server (http://localhost:5173, proxies /api → http://localhost:5000)
cd frontend && npm run dev

# Production build
cd frontend && npm run build
```

## Architecture

### Backend

- **`backend/Program.cs`** — Bootstraps DI: SQLite DbContext, JWT Bearer auth, CORS for Vue dev server (`http://localhost:5173`), Swagger, controllers.
- **`backend/Data/AppDbContext.cs`** — EF Core DbContext backed by `app.db` (SQLite).
- **`backend/Models/`** — `User` entity; DTOs are under `Models/DTOs/`.
- **`backend/Controllers/`** — `AuthController` (`POST /api/auth/register`, `POST /api/auth/login`), `UsersController`.
- **`backend/Services/TokenService.cs`** — Generates JWT tokens; config read from `appsettings.json` under `Jwt:Key/Issuer/Audience/ExpirationMinutes`.

### Frontend

- **`frontend/src/main.js`** — Mounts Vue app, registers Pinia and Vue Router.
- **`frontend/src/api/axios.js`** — Axios instance; **`frontend/src/api/auth.js`** — auth API calls.
- **`frontend/src/stores/auth.js`** — Pinia store managing login state and JWT token.
- **`frontend/src/router/index.js`** — Route definitions; route guards use the auth store.
- **`frontend/src/layouts/MainLayout.vue`** — Shell layout wrapping authenticated views.
- **`frontend/src/views/`** — `LoginView`, `RegisterView`, `UsersView`, `UserListView`, `UserAddView`, `UserEditView`.

### Auth Flow

Register/login → backend returns `{ token, user }` → Pinia auth store persists token → Axios attaches `Authorization: Bearer <token>` header → protected API routes require `[Authorize]`.

### Vite Proxy

All `/api/*` requests from the frontend dev server are proxied to `http://localhost:5000`, so no CORS issues in dev. In production you'd configure a reverse proxy (nginx/IIS) instead.
