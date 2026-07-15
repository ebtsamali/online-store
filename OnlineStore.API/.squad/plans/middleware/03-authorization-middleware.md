# Story 03 — Enable Authorization Middleware & Protect Endpoints

Complete the JWT authentication/authorization pipeline, make the JWT usable from Swagger (Authorize button), add a protected `GET /api/auth/me` probe to prove auth end-to-end, and specify the exact authorization attributes to apply to product/cart endpoints **once those controllers exist**.

---

## Prerequisites

- **Story 02 completed** ([`../auth/02-login-api.md`](../auth/02-login-api.md)): JWT **issuance** (`TokenService`) and the JWT **validation** pipeline already exist. This story builds directly on that wiring.
- **IMPORTANT — read before planning scope:** most of the intake's "Part 1" is **already implemented**. Verified in [`Program.cs`](../../../Program.cs):
  - `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` with `ValidateIssuer/Audience/Lifetime/IssuerSigningKey` reading `Jwt:Issuer/Audience/Key` — lines 33–47.
  - `app.UseAuthentication();` (line 63) **before** `app.UseAuthorization();` (line 65) **before** `app.MapControllers();` (line 67) — order is already correct.
  - So **do not re-add** the authentication block. This story only *fills the gaps* (see below).
- **BLOCKING for Part 2:** the endpoints the intake lists as protection targets **do not exist yet**. Verified: `Controllers/` contains **only** `AuthController.cs` (`grep -rl "ControllerBase" Controllers/` → one file). There is **no `ProductsController`** and **no cart controller**. The `Product` entity and `Products` `DbSet` exist, but no CRUD endpoints do. Therefore `[Authorize(Roles = "admin")]` on `POST/PUT/DELETE /api/products` **cannot be applied in this story** — those controllers must be created by a separate Products CRUD story first. This plan specifies the exact attributes to graft on when that happens; see [Deferred: Authorization matrix](#deferred--authorization-matrix-apply-when-controllers-exist).
- **Admin-user gap:** `register` hardcodes `Role = "customer"` (Story 01, `AuthController.Register`); there is currently **no code path that creates an admin**. Verifying any admin-only behaviour requires promoting a user manually in the DB (see Verification step 6).

---

## Story Goal

1. Finish the auth pipeline gaps so protected endpoints behave correctly: register authorization services explicitly and confirm middleware order.
2. Configure Swagger so testers can paste a JWT via an **Authorize** button (acceptance criterion 5).
3. Add a single **protected** endpoint now — `GET /api/auth/me` (`[Authorize]`) — so the 401-without-token behaviour is demonstrable end-to-end against a real route.
4. Document the **authorization matrix** (which future endpoint gets `[Authorize]` vs `[Authorize(Roles="admin")]` vs stays public) as a ready-to-apply contract for the Products/Cart stories.

**Not in scope:** creating `ProductsController`/CartController or any product/cart CRUD (separate stories); an admin-registration/promotion endpoint; refresh tokens; policy-based (non-role) authorization. Login/register stay **public** and unchanged.

---

## Context — Read These Files First

1. [`Program.cs`](../../../Program.cs) — 69 lines. Confirm the existing auth wiring (lines 33–47 registration; 63/65/67 middleware order). You will: (a) add `builder.Services.AddAuthorization();` after the `AddJwtBearer` block (~line 47); (b) replace the bare `AddSwaggerGen()` (line 20) with a configured overload that adds the Bearer security definition + requirement. **Do not** touch the `AddAuthentication`/`UseAuthentication`/`UseAuthorization` lines — they are already correct.
2. [`Controllers/AuthController.cs`](../../../Controllers/AuthController.cs) — current actions: `Register` (`[HttpPost("register")]`) and `Login` (`[HttpPost("login")]`). Mirror this controller's style (`[ApiController]`, `[Route("api/auth")]`, `IActionResult`) to add the `Me` action. Note it currently has **no** `using Microsoft.AspNetCore.Authorization;` — you will add it.
3. [`Services/TokenService.cs`](../../../Services/TokenService.cs) — the token is created with claims `JwtRegisteredClaimNames.Sub` (userId), `JwtRegisteredClaimNames.Email`, and `ClaimTypes.Role`. **This matters for reading claims back** (see Edge Cases — inbound claim-type mapping) and for role checks.
4. [`appsettings.json`](../../../appsettings.json) — the `Jwt` section (`Key`/`Issuer`/`Audience`) added in Story 02 is what the validation params already read. No change needed here.
5. [`OnlineStore.API.csproj`](../../../OnlineStore.API.csproj) — confirms `Swashbuckle.AspNetCore` 10.2.3 (line 22; pulls **Microsoft.OpenApi 2.7.5**, namespace `Microsoft.OpenApi`) and `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.9 (line 11). **No new packages required.**
6. [`Properties/launchSettings.json`](../../../Properties/launchSettings.json) — API at `http://localhost:5016`; Swagger at `/swagger`.
7. Precedent for tone/structure: [`../auth/02-login-api.md`](../auth/02-login-api.md).

---

## Product rules (from story)

- **Current behaviour:** the JWT pipeline validates tokens, but **no endpoint requires one** — every route is anonymous. Swagger cannot send a token.
- **New behaviour:** authorization services are explicit; Swagger can authorize; `GET /api/auth/me` requires a valid token (401 otherwise). Product/cart endpoints get their attributes when those controllers are created — per the matrix below.

---

## Implementation Tasks

### 1 — Register authorization services explicitly — `Program.cs`

**File: `Program.cs`**

Immediately **after** the `AddJwtBearer(...)` block (after line 47, before `var app = builder.Build();`):

```csharp
builder.Services.AddAuthorization();
```

> The app currently relies on the framework's implicit authorization services (that is why `UseAuthorization()` has worked since Story 01). Registering it explicitly matches the intake, documents intent, and is required once role-based `[Authorize(Roles=…)]` policies are used. It is idempotent and low-risk.

### 2 — Add the Swagger "Authorize" button — `Program.cs`

**File: `Program.cs`**

Replace the parameterless `builder.Services.AddSwaggerGen();` (line 20) with the configured overload:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT here (no 'Bearer ' prefix — Swagger adds it)."
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });
});
```

Add to the usings at the top of `Program.cs`:

```csharp
using Microsoft.OpenApi;
```

> `Type = SecuritySchemeType.Http` + `Scheme = "bearer"` makes the Swagger Authorize dialog accept the **raw** token and prepend `Bearer ` automatically. Do not use `ApiKey` type, which would require the tester to type the prefix themselves.

> **API-version note (verified against this repo's packages):** this project resolves **Microsoft.OpenApi 2.7.5** + **Swashbuckle.AspNetCore 10.2.3**, whose API differs from the older v1 pattern. The three differences that matter:
> - Types live in namespace **`Microsoft.OpenApi`** — not `Microsoft.OpenApi.Models`.
> - References use **`new OpenApiSecuritySchemeReference("Bearer", doc)`** — the old `Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }` shape no longer exists.
> - `AddSecurityRequirement` now takes a **`Func<OpenApiDocument, OpenApiSecurityRequirement>`** (hence the `doc =>` lambda), and the requirement's value type is **`List<string>`** (not `string[]`/`Array.Empty<string>()`).

### 3 — Add a protected probe endpoint — `Controllers/AuthController.cs`

**File: `Controllers/AuthController.cs`**

Add these usings (the controller does not have them yet):

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
```

Add the action below `Login`:

```csharp
[Authorize]
[HttpGet("me")]
public IActionResult Me()
{
    // NOTE: the JWT handler maps inbound "sub"->NameIdentifier and "email"->Email
    // by default, so read them via ClaimTypes (NOT JwtRegisteredClaimNames). See Edge Cases.
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = User.FindFirstValue(ClaimTypes.Email);
    var role = User.FindFirstValue(ClaimTypes.Role);

    return Ok(new { id, email, role });
}
```

> This is a genuinely useful "who am I" endpoint (the frontend can call it to hydrate session state) and it lets acceptance criterion 1 (401 without token) be verified against a real protected route **now**, without waiting for the Products story.

---

### Deferred — Authorization matrix (apply when controllers exist)

These attributes **cannot be added in this story** because the controllers/actions do not exist. When the Products CRUD story (and any Cart story) creates them, apply exactly this. Record this table in the Products story's plan as the auth contract.

| Endpoint | Attribute | Notes |
|---|---|---|
| `GET /api/products` | *(none — public)* | Anonymous catalog browsing. |
| `GET /api/products/{id}` | *(none — public)* | Anonymous. |
| `POST /api/products` | `[Authorize(Roles = "admin")]` | Admin only → 403 for customer. |
| `PUT /api/products/{id}` | `[Authorize(Roles = "admin")]` | Admin only. |
| `DELETE /api/products/{id}` | `[Authorize(Roles = "admin")]` | Admin only. |
| `GET /api/cart`, `POST /api/cart`, … | `[Authorize]` | Any authenticated user (role-agnostic). |
| `POST /api/auth/login`, `POST /api/auth/register` | *(none — public)* | Must remain anonymous. |

Prefer controller-level `[Authorize(Roles = "admin")]` with `[AllowAnonymous]` on the public GET actions when a `ProductsController` mixes public reads and admin writes.

---

## Edge Cases & Failure Modes

- **Inbound claim-type mapping (high-impact gotcha):** `JwtSecurityTokenHandler` has a `DefaultInboundClaimTypeMap` that rewrites the incoming `sub` claim to `ClaimTypes.NameIdentifier` and `email` to `ClaimTypes.Email`. So in `Me()` reading `User.FindFirstValue(JwtRegisteredClaimNames.Sub)` would return **null** — read via `ClaimTypes.NameIdentifier`/`ClaimTypes.Email` (as written in task 3). Role checks are unaffected because `JwtBearer`'s `RoleClaimType` defaults to `ClaimTypes.Role`, which is exactly the claim type `TokenService` emits.
- **No token / expired / tampered token on a protected route:** handled automatically by the middleware → **401** with a `WWW-Authenticate` header. No controller code needed. Requires task 1/existing wiring; do not add manual 401 handling in `Me()`.
- **Valid token, wrong role:** returns **403** automatically (not 401). Only observable once an admin-only endpoint exists.
- **No admin exists:** `register` always sets `Role = "customer"`. To test admin paths, promote a user in the DB: `UPDATE "Users" SET "Role" = 'admin' WHERE "Email" = 'admin@test.com';` then log in **again** to mint a token carrying `role=admin` (an old token keeps the old role until it expires).
- **HTTPS redirect vs Swagger:** run under the `http` profile (`http://localhost:5016`) for local token testing to avoid the dev-cert prompt; the Authorize button works on either profile.
- **Login/register must stay public:** adding `AddAuthorization()` and the Swagger requirement must **not** make existing endpoints require a token. The Swagger security *requirement* is a UI/documentation hint only — it does not enforce auth; enforcement is solely via `[Authorize]` attributes. Verify register/login still work anonymously (Verification step 7).

---

## Test Plan

No test project exists yet (noted in Story 02). If one is added:

1. **Integration — `Me` returns 401 without a token** (`AuthControllerMeTests.cs`): `WebApplicationFactory`, `GET /api/auth/me` with no `Authorization` header → **401**.
2. **Integration — `Me` returns 200 with claims for a valid token:** obtain a token via `TokenService`/login, call `GET /api/auth/me` with `Authorization: Bearer <token>` → **200**, body contains the correct `id`, `email`, `role`.
3. **Public routes stay open:** `POST /api/auth/register` and `POST /api/auth/login` succeed with no token (regression).
4. **(When ProductsController exists)** admin-only endpoint: customer token → 403; admin token → 2xx; no token → 401. Add alongside that controller's story.

Until a test project exists, the manual matrix in Verification Steps is the acceptance gate — record results in the PR.

---

## Verification Steps

1. **Backend builds:** `dotnet build` in `OnlineStore.API/` — must succeed, no warnings.
2. **Backend runs:** `dotnet run --launch-profile http`, open `http://localhost:5016/swagger`.
3. **Swagger Authorize button (criterion 5):** confirm an **Authorize** button appears top-right of Swagger UI.
4. **Get a token:** `POST /api/auth/login` with a seeded user (register one first if needed) → copy the `token`.
5. **401 without token:** `GET /api/auth/me` with no auth → **401**. Then click **Authorize**, paste the token, retry → **200** with `{ id, email, role }` matching the user.
6. **Admin path (requires manual promotion):** run `UPDATE "Users" SET "Role"='admin' WHERE "Email"='<you>';`, log in again, decode the new token (jwt.io) and confirm `role` claim is `admin`. *(Full 403/200 admin-endpoint check is deferred to the Products CRUD story — attributes specified in the matrix above.)*
7. **Public routes regression:** `POST /api/auth/register` and `POST /api/auth/login` still succeed **without** a token.
8. **CORS:** frontend origin (`http://localhost:3000`) preflight to `/api/auth/me` still passes (existing `"Frontend"` policy).

---

## Done Criteria

- [ ] `builder.Services.AddAuthorization()` is registered; `UseAuthentication()` → `UseAuthorization()` → `MapControllers()` order confirmed (already correct).
- [ ] Swagger UI shows an **Authorize** button and can send a Bearer token (acceptance criterion 5).
- [ ] `GET /api/auth/me` returns **401** without a token and **200** with correct `id`/`email`/`role` for a valid token (acceptance criterion 1, demonstrated on a real route).
- [ ] Public endpoints (`login`, `register`) remain accessible without a token (acceptance criterion 4).
- [ ] The authorization matrix for product/cart endpoints is recorded for the Products/Cart story (acceptance criteria 2 & 3 — admin 403 / admin 200 — are delivered there, since those endpoints do not exist yet).
- [ ] `dotnet build` succeeds; no database migration added.

---

**Scope note for the executor:** acceptance criteria 2 and 3 (admin-only 403/200) reference product endpoints that do not exist in the codebase. Do **not** invent a `ProductsController` to satisfy them — implement tasks 1–3, then report that criteria 2 & 3 are blocked on the Products CRUD story and hand over the matrix. Report to the user and wait for confirmation before proceeding to Story 04.
