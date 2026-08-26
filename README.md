# Online Store

An online store built as two independent applications: an **ASP.NET Core REST API** backed by PostgreSQL, and a **Nuxt 4 storefront** that also contains the admin area. Customers browse a product catalogue, add items to a cart and check out; administrators manage products, categories and brands from a separate `/admin` area.

---

## Architecture

| App | Path | Stack | Dev URL |
|---|---|---|---|
| **API** | [`OnlineStore.API/`](OnlineStore.API/) | .NET 10 · ASP.NET Core · EF Core 10 · PostgreSQL · JWT | `https://localhost:7225` (also `http://localhost:5016`) |
| **Frontend** | [`online-store-frontend/`](online-store-frontend/) | Nuxt 4 · Vue 3 · Pinia · Tailwind CSS | `http://localhost:3000` |

There is **no root build script and no solution-wide package manager**. The two apps are configured, built and run independently, each from its own directory. Nothing at the repository root needs installing.

### How the two halves connect

- The frontend reads its API base URL from `runtimeConfig.public.apiBase` in [`online-store-frontend/nuxt.config.ts`](online-store-frontend/nuxt.config.ts). It currently points at `https://localhost:7225/api`, which is the API's **`https`** launch profile.
- The API's CORS policy admits **only** `http://localhost:3000`, so the frontend has to run on port 3000 (`Program.cs`, the `AddCors` call).
- Authentication is a **JWT** sent as `Authorization: Bearer <token>`. The frontend keeps it in a cookie so it is readable during server-side rendering.
- Product **images** are static files served from the API host's `wwwroot` at `/images/products/…` — not from under `/api`.

If any one of those four things is misconfigured the app fails in a way that is hard to read from the browser alone. [`docs/local-setup.md`](docs/local-setup.md) walks through each.

---

## Prerequisites

| Tool | Required | Verified working |
|---|---|---|
| .NET SDK | 10.x (the project targets `net10.0`) | 10.0.400 |
| Node.js | 20+ | v24.13.0 |
| pnpm | any recent | 10.28.0 |
| PostgreSQL | any recent 14+ | — |
| `dotnet-ef` | needed for migrations | ships as a global tool |

The .NET 10 SDK is a hard requirement — `OnlineStore.API.csproj` targets `net10.0`. The Node and pnpm versions above are simply what this project has been run with; nothing in the repository pins them (there is no `.nvmrc` and no `engines` field).

Install the EF Core CLI if you do not have it:

```bash
dotnet tool install --global dotnet-ef
```

---

## Quick start

### 1. Backend

```bash
cd OnlineStore.API

# Create the database in PostgreSQL first (see docs/local-setup.md),
# then point the API at it. Prefer user-secrets over editing appsettings.json:
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=OnlineStoreDb;Username=postgres;Password=<your-password>"
dotnet user-secrets set "Jwt:Key" "<a-secret-of-at-least-32-characters>"

dotnet restore
dotnet ef database update
dotnet run --launch-profile https
```

Swagger opens at `https://localhost:7225/swagger`.

> Use the **`https`** profile. The frontend's default `apiBase` points at port 7225; the `http` profile listens on 5016 and will not be found without also changing `apiBase`.

### 2. Frontend

```bash
cd online-store-frontend
pnpm install
pnpm dev
```

Open **http://localhost:3000**.

If the catalogue renders but images are missing, or requests fail with a certificate error, see [`docs/local-setup.md`](docs/local-setup.md) — both are known first-run issues with documented fixes.

---

## Creating the first admin

Registration **always** creates a `customer`. No API endpoint can create an administrator, so the first one is promoted directly in the database:

```sql
UPDATE "Users" SET "Role" = 'admin' WHERE "Email" = 'you@example.com';
```

Then **log in again** at `/admin/login` — the role is baked into the JWT and into the frontend's session cookie at login, so an existing session keeps its old role. Full detail in [`docs/auth-and-roles.md`](docs/auth-and-roles.md).

---

## Repository layout

```
.
├── OnlineStore.API/          ASP.NET Core Web API (the backend)
│   ├── Controllers/          HTTP endpoints
│   ├── Data/                 AppDbContext — the EF Core model
│   ├── Dtos/                 Request/response records with validation attributes
│   ├── Entities/             Database entities
│   ├── Migrations/           EF Core migrations (the schema history)
│   ├── Services/             Token issuing and image storage
│   ├── wwwroot/              Static files, including uploaded product images
│   └── .squad/               squad-kit workspace for the backend
├── online-store-frontend/    Nuxt 4 storefront + admin area
│   ├── app/                  Pages, components, composables, stores, middleware
│   ├── public/               Static assets served as-is
│   └── .squad/               squad-kit workspace for the frontend
├── docs/                     Project documentation (see below)
├── .squad/                   squad-kit workspace for the repository as a whole
├── .github/                  Present but empty — there is no CI pipeline
└── .gitignore
```

---

## Documentation

| Document | What it covers |
|---|---|
| [`docs/local-setup.md`](docs/local-setup.md) | Getting a fresh clone running: PostgreSQL, configuration, ports, certificates, CORS, troubleshooting |
| [`docs/api-reference.md`](docs/api-reference.md) | All 22 endpoints — routes, authorization, request/response shapes, status codes |
| [`docs/data-model.md`](docs/data-model.md) | Entities, relationships, delete behaviour, migrations, image storage |
| [`docs/auth-and-roles.md`](docs/auth-and-roles.md) | JWT issuance, roles, backend enforcement, frontend route guards |
| [`online-store-frontend/README.md`](online-store-frontend/README.md) | The Nuxt app in detail |
| [`OnlineStore.API/README.md`](OnlineStore.API/README.md) | The API project in detail |

---

## Project conventions

This repository uses **squad-kit** to keep a written design record. Each feature is planned before it is built, and the plan stays in the repository afterwards as the explanation of why the code looks the way it does.

- **Intakes** (the raw request) live at `.squad/stories/<feature>/<story>/intake.md`.
- **Plans** (the design) live at `.squad/plans/<feature>/NN-story-<slug>.md`, with one `00-overview.md` per feature and one `00-index.md` per workspace.

There are **three independent squad-kit workspaces**, each with its own numbering sequence:

| Workspace | Scope | `NN` range so far |
|---|---|---|
| `.squad/` | The repository as a whole | 01 |
| `online-store-frontend/.squad/` | Frontend | 01–16 |
| `OnlineStore.API/.squad/` | Backend | 01–08 |

Because the sequences are independent, "Story 01" is ambiguous unless you also name the workspace.

**Before changing a feature, read its plan.** The plans record constraints that are not obvious from the code — for example why the auth-hydration plugin must be universal rather than client-only, or why `OrderItem` snapshots the product name and price instead of joining.

---

## Testing and CI

There is **no automated test suite and no CI pipeline** in this repository today:

- `online-store-frontend/package.json` has **no `test` script** and no test runner installed.
- There is **no test project** in the .NET solution.
- `.github/` exists but contains **no workflow files**.

Verification is therefore manual. Before opening a pull request:

1. `dotnet build` in `OnlineStore.API/` — must be clean.
2. `pnpm build` in `online-store-frontend/` — must be clean.
3. Work through the **Verification Steps** in the `.squad/plans/` story covering what you changed. Each plan lists the concrete scenarios to click through.

---

## Repository

<https://github.com/ebtsamali/online-store>
