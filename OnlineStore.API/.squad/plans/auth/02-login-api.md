# Story 02 — Login (User Authentication) API

Implement `POST /api/auth/login`: look up the user by email, verify the password against the stored BCrypt hash, and on success issue a signed JWT (userId, email, role; 7-day expiry) plus basic user info. Authentication failures always return the same generic **401** message.

---

## Prerequisites

- **Story 01 completed** ([`01-user-registration-api.md`](01-user-registration-api.md)): the `User` entity, `Users` `DbSet` + unique `Email` index, `AuthController` (`POST /api/auth/register`), and the `"Frontend"` CORS policy already exist and are the direct dependencies of this story. All are verified present in the current tree.
- Confirmed already available in [`OnlineStore.API.csproj`](../../../OnlineStore.API.csproj) — **no new package installs required**:
  - `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.9 (line 11). This package **transitively** provides `System.IdentityModel.Tokens.Jwt` (`JwtSecurityTokenHandler`, `SecurityTokenDescriptor`) and `Microsoft.IdentityModel.Tokens` (`SymmetricSecurityKey`, `SigningCredentials`) — the types you need for token generation. It is currently referenced but **not wired** in `Program.cs`.
  - `BCrypt.Net-Next` 4.2.0 (line 10) — use `BCrypt.Net.BCrypt.Verify(...)` (the registration path already uses `BCrypt.Net.BCrypt.HashPassword(...)`).
- **No database migration is required.** Login is read-only against the existing `Users` table (`Id`, `Name`, `Email`, `PasswordHash`, `Role` all exist via migration `20260712214535_AddUser`).
- PostgreSQL reachable at the `appsettings.json` connection string (`Host=localhost;Port=5433;Database=OnlineStoreDb`) for manual verification.

---

## Story Goal

Deliver a working login endpoint so an existing user can authenticate:

1. Client POSTs `email` + `password` to `POST /api/auth/login`.
2. Server looks up the user by email.
3. Server verifies the password against the stored `PasswordHash` with BCrypt.
4. On failure of **either** lookup or verification, server returns **401** with the **single generic** message `"Invalid email or password"` (never reveal which field was wrong).
5. On success, server generates a JWT containing `userId`, `email`, `role` (expiry **7 days**) and returns **200** with `{ token, name, email, role }`.
6. CORS already allows the Next.js frontend (`http://localhost:3000`) — reuse the existing `"Frontend"` policy, no change.

**Not in scope:** registration (done in Story 01), refresh tokens, password reset, email confirmation, logout/token revocation, and adding `[Authorize]` to any existing endpoint. This story *wires* JWT authentication into the pipeline (so issued tokens are validatable and future stories can protect endpoints) but does **not** protect any current route.

---

## Context — Read These Files First

1. [`Controllers/AuthController.cs`](../../../Controllers/AuthController.cs) — 54 lines. You will **add a `Login` action** to this existing controller. Note the established patterns to mirror: `[ApiController]` + `[Route("api/auth")]` (lines 9–10), constructor injection of `AppDbContext _db` (lines 13–18), `[HttpPost("register")]` async `Task<IActionResult>` (lines 20–21), the try / `catch (DbUpdateException)` / `catch` → `StatusCode(500, new { message = "Something went wrong" })` shape (lines 23–52), and the anonymous-object error body style `new { message = "..." }` (line 28). You will **extend the constructor** to also inject the token dependency (see task 4).
2. [`Dtos/RegisterRequest.cs`](../../../Dtos/RegisterRequest.cs) — 16 lines. DTO style to mirror: `record` with DataAnnotations directly on positional parameters (`[Required, EmailAddress] string Email`). `[ApiController]` turns annotation failures into automatic **400** `ValidationProblemDetails`. Create `LoginRequest` in the same shape.
3. [`Dtos/RegisterResponse.cs`](../../../Dtos/RegisterResponse.cs) — 3 lines. Response DTO style: a flat positional `record`. Create `AuthResponse` the same way.
4. [`Program.cs`](../../../Program.cs) — 44 lines. Current pipeline: `AddDbContext` (6–7), `AddControllers` (11), OpenAPI/Swagger (13–16), `AddCors("Frontend")` (18–24), `builder.Build()` (26), dev Swagger block (29–34), `UseHttpsRedirection` (36), `UseCors("Frontend")` (38), `UseAuthorization` (40), `MapControllers` (42), `Run` (44). **There is currently no `UseAuthentication()` call** — you will add JWT authentication registration before line 26 and `app.UseAuthentication()` immediately **before** `app.UseAuthorization()` (line 40).
5. [`Entities/User.cs`](../../../Entities/User.cs) — 11 lines. Fields available for claims/response: `Id` (int), `Name`, `Email`, `Role` (defaults `"customer"`), `PasswordHash`.
6. [`appsettings.json`](../../../appsettings.json) — 12 lines. You will add a top-level `"Jwt"` section (`Key`, `Issuer`, `Audience`) alongside `ConnectionStrings` (lines 2–4).
7. [`Properties/launchSettings.json`](../../../Properties/launchSettings.json) — API runs at `http://localhost:5016` (http profile) / `https://localhost:7225` (https profile); `launchUrl` is `swagger`. Use these for manual verification.
8. Precedent: the whole login flow parallels Story 01's controller/DTO conventions — follow [`01-user-registration-api.md`](01-user-registration-api.md) for tone, error-body shape, and verification style.

---

## Product rules (from story)

- **Current behaviour:** only registration exists; there is no way to authenticate and no token issuance anywhere in the codebase.
- **New behaviour:** `POST /api/auth/login` authenticates existing users and issues a JWT. Both "email not found" and "wrong password" return the **identical** 401 body `{ "message": "Invalid email or password" }` — the two cases must be indistinguishable to the client.

---

## Backend Tasks

### 1 — Add JWT settings to `appsettings.json`

**File: `appsettings.json`**

Add a `"Jwt"` section (sibling of `ConnectionStrings`). The signing key must be **at least 32 bytes** (256 bits) or `SymmetricSecurityKey` / HS256 throws at runtime.

```json
"Jwt": {
  "Key": "CHANGE_ME_dev_only_super_secret_key_at_least_32_chars",
  "Issuer": "OnlineStore.API",
  "Audience": "OnlineStore.Client"
}
```

- **Do not** commit a production secret here; this is a dev-only placeholder. Note in a code comment (or the PR) that the real key belongs in user-secrets / environment variables before deployment. Mirror the same section into `appsettings.Development.json` only if you want a distinct dev key; otherwise the base file suffices.

### 2 — Create the request DTO — `Dtos/LoginRequest.cs`

**Create file: `Dtos/LoginRequest.cs`**

Validation per intake: email required + valid format; password required + not empty. `[ApiController]` produces the per-field **400** automatically.

```csharp
using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record LoginRequest(
    [Required, EmailAddress]
    string Email,

    [Required]
    string Password
);
```

### 3 — Create the response DTO — `Dtos/AuthResponse.cs`

**Create file: `Dtos/AuthResponse.cs`**

Matches the intake's `AuthResponse(string Token, string Name, string Email, string Role)`.

```csharp
namespace OnlineStore.API.Dtos;

public record AuthResponse(string Token, string Name, string Email, string Role);
```

### 4 — Create a token service — `Services/TokenService.cs`

**Create file: `Services/TokenService.cs`** (new `Services/` folder; namespace `OnlineStore.API.Services`)

Encapsulate JWT creation so the controller stays thin and the logic is unit-testable. Read the key/issuer/audience from `IConfiguration`. Claims: `sub` = userId, `email`, `role`. Expiry **7 days**.

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OnlineStore.API.Entities;

namespace OnlineStore.API.Services;

public interface ITokenService
{
    string CreateToken(User user);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(User user)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

> Using `ClaimTypes.Role` (not a raw `"role"` string) means `[Authorize(Roles = "...")]` works out of the box in future stories.

### 5 — Register the token service and JWT authentication in `Program.cs`

**File: `Program.cs`**

**(a)** Before `var app = builder.Build();` (line 26) register the service and authentication:

```csharp
builder.Services.AddScoped<ITokenService, TokenService>();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });
```

Add the required usings at the top of `Program.cs`:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OnlineStore.API.Services;
```

**(b)** Add `app.UseAuthentication();` **immediately before** `app.UseAuthorization();` (currently line 40). Order matters — authentication must run before authorization:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

### 6 — Add the `Login` action to `Controllers/AuthController.cs`

**File: `Controllers/AuthController.cs`**

**(a)** Extend the constructor to inject `ITokenService` alongside the existing `AppDbContext`:

```csharp
private readonly AppDbContext _db;
private readonly ITokenService _tokenService;

public AuthController(AppDbContext db, ITokenService tokenService)
{
    _db = db;
    _tokenService = tokenService;
}
```

**(b)** Add `using OnlineStore.API.Services;` to the existing usings block (lines 1–5).

**(c)** Add the action below `Register`:

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    try
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

        // Same generic 401 whether the email is unknown OR the password is wrong.
        if (user is null ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var token = _tokenService.CreateToken(user);
        return Ok(new AuthResponse(token, user.Name, user.Email, user.Role));
    }
    catch
    {
        return StatusCode(500, new { message = "Something went wrong" });
    }
}
```

> `SingleOrDefaultAsync` is safe because `Email` has a unique index. `BCrypt.Verify` is called even on the found-user path; only the *combined* check produces the 401 so timing/branching does not leak which field failed (see Edge Cases).

---

## Edge Cases & Failure Modes

- **Email not found vs wrong password:** both must yield the exact same body `{ "message": "Invalid email or password" }` and status **401** — enforced by the single combined `if (user is null || !Verify(...))` in `Login` (task 6c). Do **not** split these into two branches with different messages.
- **Missing/short JWT key:** `SymmetricSecurityKey` requires ≥ 256-bit (32-byte) key material for HS256; a shorter or absent `Jwt:Key` throws at token creation / startup. The placeholder in task 1 is 32+ chars. If `Jwt:Key` is null the `jwt["Key"]!` null-forgiving deref will throw `ArgumentNullException` — surfaced as a **500** via the controller catch; verify the config is loaded.
- **Case-sensitive email lookup:** `u.Email == request.Email` is case-sensitive (matches Story 01's registration check and unique index, which are also case-sensitive). Consistent with registration — do not change here. **Uncertainty flagged:** if the product later wants case-insensitive login, normalize email to lower-case in *both* register and login together, not just here.
- **Validation (400):** empty email, malformed email, or empty password are rejected by `[ApiController]` model validation **before** the action body runs — never reaching the DB. This satisfies "400 → validation errors per field".
- **Password never echoed:** `AuthResponse` contains only `token`, `name`, `email`, `role` — never `PasswordHash`.
- **Token lifetime:** `expires: DateTime.UtcNow.AddDays(7)` — exactly 7 days per intake. `ValidateLifetime = true` means expired tokens are rejected once endpoints are protected (future story).
- **Clock skew:** `JwtBearer` allows a default 5-minute clock skew on lifetime validation; acceptable, no override needed.
- **No endpoint is protected yet:** adding `UseAuthentication()` must not break existing routes — `register` and any product routes remain anonymous because none carry `[Authorize]`. Verify `POST /api/auth/register` still returns 201 after the pipeline change (see Regression).

---

## Test Plan

No test project exists in the repo today. Add one so the auth logic is covered; if the team prefers manual-only for now, at minimum execute the manual matrix in Verification Steps and record results in the PR.

1. **Create test project** `OnlineStore.API.Tests` (xUnit) referencing the API project. Match .NET 10 target.
2. **Unit — `TokenService`** (`TokenServiceTests.cs`): given an in-memory `IConfiguration` with a valid `Jwt` section and a `User`, `CreateToken` returns a parseable JWT whose claims contain `sub` = `user.Id`, `email` = `user.Email`, `role` = `user.Role`, and whose `exp` is ~7 days out. Parse with `JwtSecurityTokenHandler`.
3. **Unit/Integration — `Login` action** (`AuthControllerLoginTests.cs`): use EF Core InMemory (or SQLite in-memory) `AppDbContext` seeded with one user whose `PasswordHash = BCrypt.HashPassword("Test@123")` and a stub/real `ITokenService`. Assert:
   - Correct credentials → `OkObjectResult` with `AuthResponse` (token non-empty, name/email/role correct).
   - Unknown email → `UnauthorizedObjectResult` with message `"Invalid email or password"`.
   - Wrong password → `UnauthorizedObjectResult` with the **same** message (assert the two 401 bodies are byte-identical).
4. **Validation:** a `LoginRequest` with empty email/password fails DataAnnotations (`Validator.TryValidateObject`) — mirrors the register DTO test pattern if one is added for Story 01.

---

## Verification Steps

1. **Backend builds:** run `dotnet build` in `OnlineStore.API/` — must succeed with no errors.
2. **Backend runs:** run `dotnet run` in `OnlineStore.API/`, open Swagger at `http://localhost:5016/swagger`. Confirm `POST /api/auth/login` is listed.
3. **Seed a user:** if the DB has none, `POST /api/auth/register` with `{ "name": "Sara Ahmed", "email": "sara@test.com", "password": "Test@123" }` (expect 201).
4. **Happy path (200):**
   ```
   POST http://localhost:5016/api/auth/login
   Content-Type: application/json

   { "email": "sara@test.com", "password": "Test@123" }
   ```
   Expect **200** and body `{ "token": "eyJ...", "name": "Sara Ahmed", "email": "sara@test.com", "role": "customer" }`. Paste the `token` into <https://jwt.io> (or decode locally) and confirm claims `sub`, `email`, `role` and a ~7-day `exp`.
5. **Wrong password (401):** same email, password `"wrong"` → **401** `{ "message": "Invalid email or password" }`.
6. **Unknown email (401):** `{ "email": "nobody@test.com", "password": "Test@123" }` → **401** with the **identical** body as step 5.
7. **Validation (400):** `{ "email": "not-an-email", "password": "" }` → **400** with per-field errors.
8. **Regression:** re-run `POST /api/auth/register` with a new email → still **201** (the added `UseAuthentication()` did not break the anonymous route).
9. **CORS:** a browser `fetch` from `http://localhost:3000` to `/api/auth/login` succeeds the `OPTIONS` preflight with `Access-Control-Allow-Origin` (reuses the existing `"Frontend"` policy).

---

## Done Criteria

- [ ] User can log in successfully with correct email and password and receives **200** with `{ token, name, email, role }`.
- [ ] Login is rejected with **401** `"Invalid email or password"` when the email does not exist.
- [ ] Login is rejected with the **same** **401** `"Invalid email or password"` when the password is wrong (bodies identical to the unknown-email case).
- [ ] A valid, signed JWT (HS256) containing `userId`/`sub`, `email`, `role` with a 7-day expiry is returned on success and decodes correctly.
- [ ] Missing/invalid email or empty password returns **400** with per-field validation errors.
- [ ] CORS allows the frontend origin (`http://localhost:3000`) — existing policy reused, `register` still works.
- [ ] `dotnet build` succeeds; no database migration was added.
