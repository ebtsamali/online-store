# Online Store — API

The ASP.NET Core REST API: the system's only real authorization boundary. It owns the PostgreSQL database, issues JWTs, and serves uploaded product images as static files.

For repository-wide context see the [root README](../README.md); for getting everything running see [`docs/local-setup.md`](../docs/local-setup.md).

---

## Stack

Target framework **`net10.0`**, with nullable reference types and implicit usings enabled.

| Package | Version | Role |
|---|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | EF Core provider for PostgreSQL |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.9 | Design-time support for migrations |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.9 | `dotnet ef` tooling support |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 | JWT bearer authentication |
| `BCrypt.Net-Next` | 4.2.0 | Password hashing |
| `Swashbuckle.AspNetCore` | 10.2.3 | Swagger / Swagger UI |
| `Microsoft.AspNetCore.OpenApi` | 10.0.9 | OpenAPI document generation |

---

## Requirements

- **.NET SDK 10.x** — a hard requirement, the project targets `net10.0`.
- **PostgreSQL** — 14 or newer.
- **`dotnet-ef`** — `dotnet tool install --global dotnet-ef`.

---

## Configuration

Settings are read from `appsettings.json`, then overridden by `appsettings.Development.json`, then by user secrets, then by environment variables — **later sources win**.

| Key | Purpose | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection | Shape: `Host=<host>;Port=<port>;Database=<db>;Username=<user>;Password=<password>` |
| `Jwt:Key` | Signing secret | **At least 32 characters** — HMAC-SHA256 requires a 256-bit key |
| `Jwt:Issuer` | Token issuer | `OnlineStore.API` |
| `Jwt:Audience` | Token audience | `OnlineStore.Client` |
| `Logging:LogLevel` | Log levels | Default `Information`, ASP.NET Core `Warning` |
| `AllowedHosts` | Host filtering | `*` |

Non-secret values this project expects: database **`OnlineStoreDb`** on port **`5433`** (note: *not* the PostgreSQL default 5432) with user `postgres`, and the issuer/audience pair above. `Jwt:Issuer` and `Jwt:Audience` are validated on every request, so changing either invalidates all existing tokens.

### Security note — read before deploying

`appsettings.json` in this repository contains a **development placeholder** database password and JWT signing key, and it is **committed to version control** (the root `.gitignore` does not exclude it). Anything in that file should be treated as public.

Do not add real credentials to it. Keep local secrets outside the repository:

```bash
# run in OnlineStore.API/
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=OnlineStoreDb;Username=postgres;Password=<your-password>"
dotnet user-secrets set "Jwt:Key" "<a-secret-of-at-least-32-characters>"
```

Or use environment variables, where `:` becomes `__`:

```bash
ConnectionStrings__DefaultConnection="..."
Jwt__Key="..."
```

Before any deployment: replace both values, rotate the committed ones, and move the file's secrets out of source control. Because the current values are already in git history, rotation — not just editing the file — is what actually fixes it.

---

## Running

Two launch profiles are defined in `Properties/launchSettings.json`:

| Profile | URLs | Environment | Swagger |
|---|---|---|---|
| `https` | `https://localhost:7225` and `http://localhost:5016` | `Development` | opens at launch |
| `http` | `http://localhost:5016` | `Development` | opens at launch |

```bash
dotnet restore
dotnet ef database update
dotnet run --launch-profile https
```

> **Use `https` unless you have a reason not to.** The frontend's default `apiBase` is `https://localhost:7225/api`. Running the `http` profile without also overriding `apiBase` produces connection failures with nothing useful in either log. See the profile matrix in [`docs/local-setup.md`](../docs/local-setup.md).

Both profiles set `ASPNETCORE_ENVIRONMENT=Development`, which matters — see the pipeline section below.

---

## Swagger

Swagger UI is registered **only in the Development environment**, at `/swagger`.

To call a protected endpoint from the UI, click **Authorize** and paste the raw JWT from a login response — Swagger adds the prefix itself. The security definition says so explicitly:

> "Paste your JWT here (no 'Bearer ' prefix — Swagger adds it)."

A JWT security requirement is applied to the whole document, so the padlock appears on every operation, including the anonymous ones. Check the authorization column in [`docs/api-reference.md`](../docs/api-reference.md) for what actually requires a token.

---

## Database and migrations

The schema is managed **exclusively** through EF Core migrations. Never modify the database by hand — the next `database update` will disagree with it.

```bash
dotnet ef database update              # apply all pending migrations
dotnet ef migrations add <Name>        # scaffold a new migration from model changes
dotnet ef migrations list              # show applied/pending
dotnet ef migrations remove            # drop the last (unapplied) migration
```

Six migrations exist, in order:

| Migration | Added |
|---|---|
| `InitialCreate` | `Products` table |
| `AddUser` | `Users` table with a unique index on `Email` |
| `AddCategoryBrandAndProductFks` | `Categories` and `Brands` tables; `Product.CategoryId`/`BrandId` with foreign keys |
| `AddCartItem` | `CartItems` table |
| `AddProductImageUrl` | `Product.ImageUrl` column |
| `AddOrders` | `Orders` and `OrderItems` tables |

`Migrations/AppDbContextModelSnapshot.cs` is generated — **never hand-edit it**. Full schema detail is in [`docs/data-model.md`](../docs/data-model.md).

---

## Project structure

| Directory | Contents |
|---|---|
| `Controllers/` | Six controllers, one per resource: `Auth`, `Products`, `Categories`, `Brands`, `Cart`, `Orders` |
| `Dtos/` | Request and response `record`s. Validation lives here as data annotations. |
| `Entities/` | EF Core entities — the persisted shape |
| `Data/` | `AppDbContext`: `DbSet`s, indexes, relationships, delete behaviour |
| `Migrations/` | Generated migrations and the model snapshot |
| `Services/` | `TokenService` (JWT issuing) and `ImageStorageService` (upload validation and file I/O) |
| `Properties/` | `launchSettings.json` |
| `wwwroot/` | Static files. Uploaded product images land in `wwwroot/images/products/`. |

DTOs and entities are kept deliberately separate: controllers never return an entity directly, so the persisted shape can change without altering the API contract.

---

## Request pipeline

In order, from `Program.cs`:

1. **Swagger** — `MapOpenApi()`, `UseSwagger()`, `UseSwaggerUI()`, **Development only**.
2. **HTTPS redirection** — `UseHttpsRedirection()`, **only when *not* Development**. In development both HTTP and HTTPS are served as-is.
3. **Static files** — `UseStaticFiles()`. This is what makes `/images/products/…` reachable.
4. **CORS** — the policy named `Frontend`, which allows **only** the origin `http://localhost:3000`, with any header and any method.
5. **Authentication** — JWT bearer validation.
6. **Authorization** — evaluates `[Authorize]` attributes.
7. **Controllers** — `MapControllers()`.

Two consequences worth remembering: Swagger is unavailable in any non-Development environment, and the CORS origin is hard-coded, so a frontend on any other port is rejected by the browser before the request reaches a controller.

---

## Conventions

**Error envelope.** Every controller action wraps its body in `try`/`catch` and returns `500` with `{ "message": "Something went wrong" }` on an unhandled exception. Deliberate failures use the same envelope with a specific message, e.g. `{ "message": "Product not found" }`.

**Validation.** Request validation is declarative, via data annotations on the DTOs. Because the controllers are `[ApiController]`, a failing annotation short-circuits into an automatic `400` carrying a `ValidationProblemDetails` body — that path is not hand-written anywhere.

**Status codes.**

| Code | Meaning here |
|---|---|
| `400` | Validation failure, or a referenced entity does not exist (e.g. unknown `CategoryId`) |
| `401` | Missing/invalid token, or bad credentials at login |
| `403` | Authenticated but not permitted — wrong role, or another user's row |
| `404` | The addressed resource does not exist |
| `409` | Conflict — duplicate email, or deleting a category still in use |
| `500` | Unhandled exception |

**Server-owned fields.** Client input never sets `IsActive`, `CreatedAt` or `Role` on create. They come from entity defaults, so a crafted request cannot self-promote to admin or backdate a record.

---

## Image uploads

Product create and update accept `multipart/form-data`. `ImageStorageService` enforces:

| Rule | Value |
|---|---|
| Allowed extensions | `.jpg`, `.jpeg`, `.png`, `.webp` |
| Maximum size | 5 MB |
| Storage directory | `wwwroot/images/products/` |
| Stored filename | a fresh GUID plus the original extension |
| Public URL | `/images/products/{guid}{ext}` |

A rejected file raises `InvalidImageException`, which the controller converts to a `400` carrying the specific reason.

File writes are ordered to avoid orphans and data loss:

- **Create** — validate the image, then verify the category and brand exist, and only then write the file. If the database save fails afterwards, the new file is deleted.
- **Update** — write the new file, save, and delete the **old** file only after the save succeeds. If the save fails, the newly written file is removed and the original is untouched.
- **Delete** — remove the row first, then the file, so a failed delete cannot leave a row pointing at a missing image.

Only the filename is derived from the upload; the client's original filename never reaches the filesystem.

---

## Related documentation

- [`docs/api-reference.md`](../docs/api-reference.md) — every endpoint in detail
- [`docs/data-model.md`](../docs/data-model.md) — entities, relationships, migrations
- [`docs/auth-and-roles.md`](../docs/auth-and-roles.md) — JWT claims, roles, enforcement
- [`docs/local-setup.md`](../docs/local-setup.md) — environment setup and troubleshooting
- [`.squad/plans/`](.squad/plans/) — the design record for each backend feature

---

## Testing

**There is no test project in this solution.** Verification is:

1. `dotnet build` — must be clean.
2. Exercising the affected endpoints through Swagger at `/swagger`.
3. The manual **Verification Steps** in the relevant story under [`.squad/plans/`](.squad/plans/).
