# Story 01 — Sign Up (User Registration) API

Implement `POST /api/auth/register`: validate input, reject duplicate emails, hash the password with BCrypt, persist a `User`, and return a plain success confirmation (no JWT).

---

## Prerequisites

- **None.** This is the first planned story for the `auth` feature and the first controller in the project (`Controllers/` is currently empty).
- Confirmed already available in [`OnlineStore.API.csproj`](../../../OnlineStore.API.csproj): `BCrypt.Net-Next` 4.2.0 and `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 — **no new package installs are required**. (`Microsoft.AspNetCore.Authentication.JwtBearer` is present but out of scope here; do not wire JWT.)
- PostgreSQL must be reachable at the connection string in `appsettings.json` (`Host=localhost;Port=5433;Database=OnlineStoreDb`) for the migration and manual verification steps.

---

## Story Goal

Deliver a working registration endpoint so a new user can create an account:

1. Client POSTs name, email, password to `POST /api/auth/register`.
2. Server validates the fields (name length, email format, password strength).
3. Server rejects the request with **409 Conflict** if the email is already registered.
4. Server hashes the password with BCrypt (plain text is never stored).
5. Server saves a `User` with `Role = "customer"` and `CreatedAt = DateTime.UtcNow`.
6. Server returns **201 Created** with a confirmation body (message, name, email) — **no JWT token**.
7. CORS is enabled so the Next.js frontend (`http://localhost:3000`) can call the API.

**Not in scope:** login, JWT issuance/validation, password reset, email confirmation, roles beyond the default `"customer"`, refresh tokens. Login is a separate follow-up story.

---

## Context — Read These Files First

1. [`Program.cs`](../../../Program.cs) — the entire file is ~34 lines. Note the current pipeline order: `AddDbContext<AppDbContext>` (lines 6–7), `AddControllers()` (line 11), `builder.Build()` (line 18), the Development-only Swagger/OpenAPI block (lines 21–26), `UseHttpsRedirection()` (line 28), `UseAuthorization()` (line 30), `MapControllers()` (line 32), `app.Run()` (line 34). You will add a CORS service registration **before** line 18 and a `UseCors(...)` call **after** line 28 and **before** `UseAuthorization()` (line 30).
2. [`Data/AppDbContext.cs`](../../../Data/AppDbContext.cs) — 13 lines. Follow the exact pattern of `public DbSet<Product> Products => Set<Product>();` (line 12) to add a `Users` `DbSet`, and add a unique-index configuration for `User.Email` via `OnModelCreating`.
3. [`Entities/Product.cs`](../../../Entities/Product.cs) — 12 lines. This is the entity style to mirror: namespace `OnlineStore.API.Entities`, non-nullable `string` props initialized to `string.Empty`, `DateTime CreatedAt { get; set; } = DateTime.UtcNow;` (line 11), `int Id` PK (line 5). Create `Entities/User.cs` in the same shape.
4. [`Migrations/20260711160101_InitialCreate.cs`](../../../Migrations/20260711160101_InitialCreate.cs) — read `Up`/`Down` (lines 13–39) to see the Npgsql column conventions this project generates (`type: "text"`, `type: "integer"` with `NpgsqlValueGenerationStrategy.IdentityByDefaultColumn` for the PK, `timestamp with time zone` for `DateTime`). Your new migration will be auto-generated and should look like this for the `Users` table plus a unique index on `Email`. **Do not hand-write it** — generate it with `dotnet ef` (see Implementation Steps).
5. [`appsettings.json`](../../../appsettings.json) — confirm `ConnectionStrings:DefaultConnection` (line 3) is the DB the migration targets.
6. [`Properties/launchSettings.json`](../../../Properties/launchSettings.json) — the API runs at `http://localhost:5016` and `https://localhost:7225`; `launchUrl` is `swagger`. Use these URLs for manual verification.
7. Grep for existing controllers before creating one: `grep -r "ControllerBase" Controllers/` — expect **no matches** (the folder is empty), so `AuthController` will be the first.

---

## Implementation Steps

### 1. Create the `User` entity — `Entities/User.cs`

Mirror `Entities/Product.cs`. Namespace `OnlineStore.API.Entities`.

```csharp
namespace OnlineStore.API.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "customer";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 2. Register the entity in `Data/AppDbContext.cs`

- Add `public DbSet<User> Users => Set<User>();` following the `Products` line (line 12).
- Add a unique index on `Email` so DB-level uniqueness is enforced (defense in depth beyond the app check). Override `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique();
}
```

### 3. Create request/response DTOs

Create `Dtos/RegisterRequest.cs` and `Dtos/RegisterResponse.cs` (new `Dtos/` folder; namespace `OnlineStore.API.Dtos`). Use records with DataAnnotations so `[ApiController]` performs automatic 400 validation:

```csharp
using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record RegisterRequest(
    [property: Required, StringLength(50, MinimumLength = 3)]
    string Name,

    [property: Required, EmailAddress]
    string Email,

    [property: Required, MinLength(8)]
    [property: RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter and one number.")]
    string Password
);
```

```csharp
namespace OnlineStore.API.Dtos;

public record RegisterResponse(string Message, string Name, string Email);
```

> Note: `[ApiController]` auto-returns **400** with a per-field `ValidationProblemDetails` when annotations fail — this satisfies the "400 → validation errors per field" requirement without manual code.

### 4. Create `Controllers/AuthController.cs`

- Namespace `OnlineStore.API.Controllers`; `[ApiController]`, `[Route("api/auth")]`, inherit `ControllerBase`.
- Inject `AppDbContext` via constructor (same DI pattern the `AddDbContext` registration in `Program.cs` line 6 enables).
- Action `Register`: `[HttpPost("register")]`, `async Task<IActionResult>`, accepts `RegisterRequest`.

Logic:

1. Check duplicate: `await _db.Users.AnyAsync(u => u.Email == request.Email)` → if true, `return Conflict(new { message = "Email already exists" });` (**409**).
2. Hash: `var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);` (namespace `BCrypt.Net`).
3. Build `User` (`Name`, `Email`, `PasswordHash = hash`; leave `Role`/`CreatedAt` to entity defaults), `_db.Users.Add(user)`, `await _db.SaveChangesAsync()`.
4. Return **201**: `return CreatedAtAction(nameof(Register), new RegisterResponse("Account created successfully", user.Name, user.Email));` — or `StatusCode(201, response)` if you prefer no Location header.
5. Wrap the DB work in try/catch; on unexpected exception return `StatusCode(500, new { message = "Something went wrong" })`.

Add `using Microsoft.EntityFrameworkCore;`, `using OnlineStore.API.Data;`, `using OnlineStore.API.Dtos;`, `using OnlineStore.API.Entities;`.

### 5. Enable CORS in `Program.cs`

- **Before** `var app = builder.Build();` (line 18) register a named policy:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
```

- After `app.UseHttpsRedirection();` (line 28) and **before** `app.UseAuthorization();` (line 30) add:

```csharp
app.UseCors("Frontend");
```

> Frontend origin: repo is `online-store-net-next` (Next.js), whose dev server defaults to `http://localhost:3000`. The intake's acceptance criterion lists `localhost:` with no port — `3000` is the assumed value; see Edge Cases if the real port differs.

### 6. Generate and apply the EF Core migration

From the project root (`OnlineStore.API/`):

```powershell
dotnet ef migrations add AddUser
dotnet ef database update
```

Inspect the generated `Migrations/*_AddUser.cs` — it should `CreateTable("Users", ...)` with the same column types shown in `InitialCreate.cs` (lines 19–26) plus `CreateIndex` on `Email` with `unique: true`. If `dotnet ef` is missing, install with `dotnet tool install --global dotnet-ef`.

---

## Edge Cases & Failure Modes

- **Race on duplicate email:** two concurrent requests can both pass the `AnyAsync` check, then one `SaveChangesAsync` throws `DbUpdateException` on the unique index. The try/catch must catch this and return **409** (`"Email already exists"`), not 500. Prefer catching `DbUpdateException` specifically before the generic 500 catch.
- **Email casing:** `sara@test.com` vs `Sara@Test.com` are treated as distinct by the check and index as written. The intake does not specify case-insensitive email. **Uncertainty flagged:** if case-insensitive uniqueness is required, normalize `Email` to lower-case before both the `AnyAsync` check and `Add`. Default to storing as-provided unless told otherwise.
- **Frontend port:** acceptance criterion 5 says `localhost:` with the port blank. Plan assumes `3000` (Next.js default). Confirm the actual dev port; adjust `WithOrigins` if different. Do **not** use `AllowAnyOrigin()` since credentials/headers policies and real deployment origins should be explicit.
- **Password never echoed:** ensure `RegisterResponse` contains only message/name/email — never `PasswordHash` or the raw password.
- **No JWT:** confirm no token generation is added; the response is confirmation-only per the intake.

---

## Verification

1. **Build:** `dotnet build` — must succeed with no errors.
2. **Migration applied:** `dotnet ef database update` completes; the `Users` table and unique `Email` index exist in `OnlineStoreDb`.
3. **Run:** `dotnet run`, then open Swagger at `http://localhost:5016/swagger`.
4. **Happy path (201):**

   ```
   POST http://localhost:5016/api/auth/register
   Content-Type: application/json

   { "name": "Sara Ahmed", "email": "sara@test.com", "password": "Test@123" }
   ```

   Expect **201** and body `{ "message": "Account created successfully", "name": "Sara Ahmed", "email": "sara@test.com" }`.

5. **Duplicate (409):** POST the same email again → **409** `{ "message": "Email already exists" }`.
6. **Validation (400):** POST `{ "name": "Al", "email": "not-an-email", "password": "abc" }` → **400** with per-field errors (name too short, invalid email, weak password).
7. **Hash check:** query `SELECT "Email", "PasswordHash", "Role" FROM "Users";` — `PasswordHash` is a BCrypt string (starts with `$2`), never the plain password; `Role` is `customer`.
8. **CORS:** from the frontend origin (or a browser fetch from `http://localhost:3000`), a preflight `OPTIONS` to `/api/auth/register` succeeds with the `Access-Control-Allow-Origin` header.

---

## Files Touched

| File                            | Change                                                          |
| ------------------------------- | --------------------------------------------------------------- |
| `Entities/User.cs`              | **New** — `User` entity                                         |
| `Dtos/RegisterRequest.cs`       | **New** — request DTO with validation annotations               |
| `Dtos/RegisterResponse.cs`      | **New** — response DTO record                                   |
| `Controllers/AuthController.cs` | **New** — `POST /api/auth/register`                             |
| `Data/AppDbContext.cs`          | Add `Users` `DbSet` + unique `Email` index in `OnModelCreating` |
| `Program.cs`                    | Add CORS policy registration + `UseCors("Frontend")`            |
| `Migrations/*_AddUser.cs`       | **New (generated)** — `Users` table + unique index              |
