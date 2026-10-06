# Team Task Management System

A role-based, full-stack task management application built with **ASP.NET Core (.NET 10)**, **Entity Framework Core**, **SQL Server** and **React + Axios**.

Organizations can manage teams, create and assign tasks, track progress (To Do → In Progress → Done), collaborate through comments, and receive notifications when tasks are assigned or their status changes.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core Web API (.NET 10), C# |
| API style | REST, JWT bearer auth, role-based authorization, DataAnnotations validation, RFC 7807 ProblemDetails errors |
| Database | Microsoft SQL Server (LocalDB for dev, SQL Server 2022 in Docker) |
| ORM | Entity Framework Core 10 (code-first migrations) |
| Auth | JWT (HMAC-SHA256) with expiry, BCrypt password hashing |
| Frontend | React 18, React Router 6, Axios, Vite |
| Docs | Swagger / OpenAPI (Swashbuckle) + Postman collection |
| Testing | xUnit, EF Core InMemory, `WebApplicationFactory` integration tests |
| DevOps | Docker (multi-container: SQL Server + API + nginx/React), GitHub Actions CI |

---

## Features

### Authentication & Authorization
- Register / log in with **JWT** tokens (60-minute expiry, configurable).
- Passwords hashed with **BCrypt**; strong-password validation.
- **Token expiration handling**: the API returns `401` with a `Token-Expired` header; the React app logs the user out automatically with a "session expired" message (also on a client-side timer).
- **Role-based access control** with `[Authorize(Roles = ...)]` plus per-resource checks in the service layer.

### Roles

| Role | What they can do |
|------|-----------------|
| **Admin** | Manage users & roles, create/edit/delete teams, assign managers and members to teams, create tasks and assign them to **Managers and Users**, see everything. |
| **Manager** | Create tasks, assign them to members of the teams they manage (or themselves), edit/delete their tasks, add unassigned users to / remove users from their own teams, see their teams' tasks. |
| **User** | See tasks assigned to them, update their status, comment on them, see their own team. |

### Core
- Task CRUD with title, description, **priority** (Low/Medium/High), **deadline**, assignee and team.
- Status tracking: **To Do, In Progress, Done**; overdue detection.
- Team management (Admin) and team membership management (Admin + team Manager).
- **Comments** on tasks for collaboration (authors/Admin can delete).

### Notifications
Triggered on **task assignment** and **task status update**:
- Stored as **in-app notifications** (bell icon with unread count, mark read / mark all read).
- **Email**: a mock sender logs emails to the console by default (`[MOCK EMAIL] ...`). Set `Email:Enabled=true` + SMTP settings to send real emails.

### Dashboard
- Totals per status, overdue count, due-in-7-days, completion %, breakdown by priority.
- **Task status overview per user** (table with progress bars).
- Upcoming deadlines.
- **Filtering by deadline range, status and priority** (task list also supports search, sorting, "assigned to me" and paging).

---

## Project Structure

```
TaskManagementSystem/
├── TaskManager.sln
├── docker-compose.yml
├── .github/workflows/ci.yml
├── docs/
│   ├── TaskManager.postman_collection.json
│   └── swagger.json
├── backend/
│   ├── Dockerfile
│   ├── TaskManager.Api/
│   │   ├── Controllers/        Auth, Users, Teams, Tasks (+comments), Notifications, Dashboard
│   │   ├── Services/           Business logic & RBAC rules, JWT, notifications/email
│   │   ├── Data/               AppDbContext, DbSeeder, Migrations
│   │   ├── Models/             Entities & enums
│   │   ├── DTOs/               Request/response contracts with validation
│   │   ├── Common/             Exceptions, global error middleware, claims helpers
│   │   └── Program.cs
│   └── TaskManager.Tests/
│       ├── Unit/               Service-level tests (permissions, notifications, filters)
│       └── Integration/        Full HTTP pipeline tests (auth, RBAC, flows)
└── frontend/
    ├── Dockerfile, nginx.conf
    └── src/
        ├── api/                Axios client (JWT interceptor, 401 handling) & API services
        ├── context/            AuthContext (session, expiry)
        ├── components/         Layout, NotificationBell, TaskFormModal, filters, common UI
        └── pages/              Login, Register, Dashboard, Tasks, TaskDetail, Teams, Users
```

### Database Design

```
Users ──< Tasks (AssignedTo)          Users >── Teams (member, TeamId)
Users ──< Tasks (CreatedBy)           Teams >── Users (Manager, ManagerId)
Teams ──< Tasks                       Tasks ──< Comments >── Users (Author)
Users ──< Notifications >── Tasks
```

| Table | Key columns |
|-------|------------|
| Users | Id, FullName, Email (unique), PasswordHash, Role, TeamId → Teams |
| Teams | Id, Name (unique), Description, ManagerId → Users |
| Tasks | Id, Title, Description, Status, Priority, DueDate, CreatedById → Users, AssignedToId → Users, TeamId → Teams |
| Comments | Id, Content, TaskItemId → Tasks (cascade), AuthorId → Users |
| Notifications | Id, Type, Message, IsRead, UserId → Users (cascade), TaskItemId → Tasks |

Indexes on `Users.Email`, `Teams.Name`, `Tasks.Status`, `Tasks.DueDate`, `Notifications(UserId, IsRead)`. Enums are stored as readable strings.

---

## Getting Started (local)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server — **LocalDB** (ships with Visual Studio) or SQL Server Express / Developer
- [Node.js 20+](https://nodejs.org/)

### 1. Backend API

```bash
cd backend/TaskManager.Api
dotnet run --launch-profile http
```

- On first start the API **applies EF Core migrations automatically** and **seeds demo data**.
- Default connection string (in `appsettings.json`) uses LocalDB:
  `Server=(localdb)\MSSQLLocalDB;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True`
  Change it if you use SQL Server Express, e.g. `Server=.\SQLEXPRESS;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True`.
- API: http://localhost:5000 · **Swagger UI: http://localhost:5000/swagger**

In **Visual Studio**: open `TaskManager.sln`, set `TaskManager.Api` as the startup project and press **F5**.

Optional – manage migrations manually:

```bash
dotnet tool restore
dotnet ef database update --project backend/TaskManager.Api
dotnet ef migrations add <Name> --project backend/TaskManager.Api -o Data/Migrations
```

### 2. Frontend

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173**. Vite proxies `/api` requests to `http://localhost:5000`.

### 3. Tests

```bash
dotnet test TaskManager.sln
```

26 tests: unit tests for task/team business rules (permissions, notifications, filtering, overdue logic) and integration tests that boot the real API in memory (auth, 401/403 handling, validation, the full create → assign → status update → comment → notify flow, dashboard, Swagger).

---

## Run with Docker (multi-container)

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| React app (nginx) | http://localhost:3000 |
| API + Swagger | http://localhost:8080/swagger |
| SQL Server | localhost,1433 (user `sa`, password `YourStrong!Passw0rd`) |

Override secrets with environment variables `SA_PASSWORD` and `JWT_KEY` (or a `.env` file next to `docker-compose.yml`).

---

## Sample Credentials

All demo accounts use the password **`Password@123`**.

| Role | Email | Notes |
|------|-------|-------|
| Admin | admin@taskmanager.com | Full access |
| Manager | manager@taskmanager.com | Manages **Engineering** (Uma, Ravi) |
| Manager | manager2@taskmanager.com | Manages **Marketing** (Sara) |
| User | user@taskmanager.com | Uma – Engineering |
| User | ravi@taskmanager.com | Ravi – Engineering |
| User | sara@taskmanager.com | Sara – Marketing |

The login page has one-click buttons to fill the Admin / Manager / User accounts.

---

## API Documentation

- **Swagger UI**: `/swagger` (click **Authorize** and paste the token from `POST /api/auth/login`).
- **Postman**: import `docs/TaskManager.postman_collection.json`. Run a *Login* request first – the token is saved automatically.
- OpenAPI JSON export: `docs/swagger.json`.

| Method | Endpoint | Roles | Description |
|--------|----------|-------|-------------|
| POST | /api/auth/register | Public | Register (role User) |
| POST | /api/auth/login | Public | Login → JWT |
| GET | /api/auth/me | Any | Current profile |
| GET | /api/tasks | Any | List visible tasks (`status, priority, dueFrom, dueTo, overdue, assignedToId, teamId, search, sortBy, desc, page, pageSize`) |
| GET | /api/tasks/{id} | Any (with access) | Task details |
| POST | /api/tasks | Admin, Manager | Create task (+ assignment notification) |
| PUT | /api/tasks/{id} | Admin, Manager | Edit task |
| PATCH | /api/tasks/{id}/assign | Admin, Manager | Assign / unassign (+ notification) |
| PATCH | /api/tasks/{id}/status | Any (assignee/manager/admin) | Change status (+ notification) |
| DELETE | /api/tasks/{id} | Admin, Manager | Delete task |
| GET/POST | /api/tasks/{id}/comments | Any (with access) | List / add comments |
| DELETE | /api/tasks/{id}/comments/{commentId} | Author, Admin | Delete comment |
| GET | /api/teams | Any | Teams visible to the caller |
| POST/PUT/DELETE | /api/teams[/{id}] | Admin | Manage teams |
| POST | /api/teams/{id}/members | Admin, team Manager | Add member |
| DELETE | /api/teams/{id}/members/{userId} | Admin, team Manager | Remove member |
| GET | /api/users | Admin, Manager | List users (`role, unassigned`) |
| POST | /api/users | Admin | Create user with any role |
| PUT | /api/users/{id}/role | Admin | Change role |
| DELETE | /api/users/{id} | Admin | Delete user |
| GET | /api/notifications | Any | My notifications |
| GET | /api/notifications/unread-count | Any | Unread count |
| PATCH | /api/notifications/{id}/read, /read-all | Any | Mark as read |
| GET | /api/dashboard | Any | Dashboard stats (same filters as tasks) |
| GET | /health | Public | Health check |

**Error format** (all errors):

```json
{ "status": 403, "title": "Forbidden", "detail": "Managers can only assign tasks to members of their own teams." }
```

Validation errors return `400` with an `errors` dictionary per field.

---

## Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string | LocalDB |
| `Jwt:Key` | Signing key (≥ 32 chars) – **change in production** | dev key |
| `Jwt:ExpiryMinutes` | Token lifetime | 60 |
| `Email:Enabled` | `true` to send real emails via SMTP; otherwise mock (logged) | false |
| `Email:SmtpHost/SmtpPort/Username/Password/From` | SMTP settings | — |
| `Cors:Origins` | Allowed frontend origins | localhost:5173, :3000 |
| `Database:Seed` | Seed demo data on empty DB | true |
| `Swagger:Enabled` | Expose Swagger UI | true |

Environment variables use `__` as separator, e.g. `Jwt__Key`, `ConnectionStrings__DefaultConnection`.

---

## CI/CD

`.github/workflows/ci.yml` runs on every push / PR:
1. **Backend** – restore, build, run all xUnit tests (with test results + coverage artifacts).
2. **Frontend** – `npm ci` and production build.
3. **Docker** – builds both images.

### Deployment (optional)
- **API** → Render / Railway as a Docker web service from `backend/Dockerfile`; set `ConnectionStrings__DefaultConnection` (e.g. Azure SQL / any SQL Server), `Jwt__Key`, `Cors__Origins__0`.
- **Frontend** → Netlify / Vercel: build command `npm run build`, publish dir `dist`, env var `VITE_API_URL=https://<your-api-host>`, and add an SPA rewrite to `index.html`.

---

## Accessibility & Responsiveness
- Semantic landmarks, skip link, labelled form controls, `aria-live` status messages, keyboard-accessible modals (Esc to close, focus restore) and notification menu.
- Visible focus rings, colour + text for status, `prefers-color-scheme` dark mode, `prefers-reduced-motion`.
- Responsive layout: collapsible navigation, stacked grids and simplified tables on phones.
