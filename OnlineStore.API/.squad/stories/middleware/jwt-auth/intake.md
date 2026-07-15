# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/middleware/jwt-auth/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `middleware`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `jwt-auth` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
jwt-auth
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Task: Enable Authorization Middleware & Protect Endpoints

Description:
Currently, Login and Sign Up are working and return a JWT token, but no 
endpoint actually validates or requires that token. This task sets up JWT 
authentication middleware in the application and applies role-based 
authorization to protect specific endpoints.

Part 1 — Configure JWT Authentication Middleware (Program.cs)

1. Register JWT Bearer authentication using the same secret key, issuer, 
   and audience that were used in TokenService to generate tokens.
2. Add authentication and authorization middleware to the request pipeline.
3. Ensure the middleware order is correct:
   - UseAuthentication() must be called BEFORE UseAuthorization()
   - Both must be called BEFORE MapControllers()

Example configuration:

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

// After app.Build():
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Part 2 — Apply Authorization Attributes on Endpoints

Use [Authorize] on any endpoint that requires the user to be logged in 
(any role).

Use [Authorize(Roles = "admin")] on any endpoint that should only be 
accessible by an admin user.

Endpoints requiring [Authorize] (any logged-in user):
- (list endpoints here once defined, e.g. GET /api/cart, POST /api/cart)

Endpoints requiring [Authorize(Roles = "admin")]:
- POST /api/products (create product)
- PUT /api/products/{id} (update product)
- DELETE /api/products/{id} (delete product)

Endpoints that remain public (no [Authorize] needed):
- GET /api/products
- GET /api/products/{id}
- POST /api/auth/login
- POST /api/auth/register

Error handling:
- If a request has no token or an invalid/expired token on a protected 
  endpoint → return 401 Unauthorized automatically (handled by the 
  middleware, no extra code needed).
- If a request has a valid token but the wrong role (e.g. a "customer" 
  trying to access an admin-only endpoint) → return 403 Forbidden 
  automatically.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] Calling a protected endpoint without a token returns 401 Unauthorized.
- [ ] Calling an admin-only endpoint with a "customer" token returns 
      403 Forbidden.
- [ ] Calling an admin-only endpoint with an "admin" token succeeds.
- [ ] Public endpoints (product listing, login, register) remain accessible 
      without a token.
- [ ] Swagger UI shows an "Authorize" button allowing manual token testing.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| *(e.g. `attachments/flow.png`)* | *(e.g. UX flow)* |

*(Add rows per file. If none, write "None.")*

---

## Dependencies

- **Blocked by / related ids:** (tracker ids only; optional short note)
- **Depends on code areas or other stories:**

## Extra notes (optional)

- 

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `c#`.

## Out of scope

- What this story explicitly does **not** cover:
