# Story 01 — Repository README, frontend README & backend documentation

Write the project's documentation set from scratch: a repository-root `README.md`, a real frontend `README.md` replacing the stock Nuxt starter text, a backend `README.md`, and four deep-dive documents under `docs/`. Target reader: a developer who has just cloned the repo and has never seen it before.

---

## Prerequisites

- **None** in terms of code. All documented behaviour already exists and is verified against the sources cited below.
- **Read for tone and as source material** (this is the first plan in the repo-root `.squad` workspace, so precedent lives in the two sibling workspaces):
  - [`../../../online-store-frontend/.squad/plans/middelware/16-story-guest-guard-and-admin-route-separation.md`](../../../online-store-frontend/.squad/plans/middelware/16-story-guest-guard-and-admin-route-separation.md) — the current, authoritative description of frontend route protection. The auth/roles document must agree with it.
  - [`../../../OnlineStore.API/.squad/plans/00-index.md`](../../../OnlineStore.API/.squad/plans/00-index.md) — the backend's own six planned features (`auth`, `middleware`, `products`, `cart`, `orders`, `categories-and-brands`); use it to cross-check that the API reference covers every shipped area.
- **Coordination:** none required — documentation only. **No application source file is modified by this story.**

---

## Story Goal

Produce seven documents so that a new contributor can clone, configure, run both halves, and understand the domain without reading the source first:

1. **`README.md`** (repo root — currently **does not exist**) — the entry point: what the product is, the two-app architecture, prerequisites, a copy-pasteable quick start for both halves, repo layout, and links to everything else.
2. **`online-store-frontend/README.md`** — replace the stock "Nuxt Minimal Starter" boilerplate with a real guide to the Nuxt app.
3. **`OnlineStore.API/README.md`** — a real guide to the ASP.NET Core API.
4. **`docs/local-setup.md`** — the full environment setup, including PostgreSQL, the non-default DB port, the launch-profile/`apiBase` pairing, CORS, and the self-signed-certificate problem.
5. **`docs/api-reference.md`** — every endpoint: route, verb, authorization, request shape, response shape, and status codes.
6. **`docs/data-model.md`** — entities, relationships, delete behaviours, migrations, and image storage.
7. **`docs/auth-and-roles.md`** — the end-to-end auth story across both halves: JWT issuance and claims, role assignment, backend enforcement, frontend guards, and why the frontend guards are not security.

**Not in scope:** changing any application code, configuration, or `.gitignore`; adding CI workflows (the root `.github/` directory exists but contains **no files** — state that there is no CI rather than adding one); rotating the committed secrets (documented as a risk with remediation steps, but the fix is a separate story); generating API docs automatically from OpenAPI; screenshots or diagrams beyond Markdown tables and fenced code.

---

## Context — Read These Files First

Every fact the docs assert must trace to one of these. Line numbers below are verified.

**Backend — wiring and configuration**

1. `OnlineStore.API/Program.cs` — the whole file is 97 lines; read it all. Key regions: DbContext/Npgsql registration **lines 11–12**; Swagger + JWT bearer security definition **lines 21–40**; the CORS policy named `"Frontend"` allowing **only** `http://localhost:3000` **lines 42–48**; DI for `ITokenService`/`IImageStorageService` **lines 50–51**; `TokenValidationParameters` **lines 57–67**; Swagger mapped **only** in Development **lines 75–80**; `UseHttpsRedirection` **only when not** Development **lines 82–85**; `UseStaticFiles()` **line 87** (this is what serves product images).
2. `OnlineStore.API/OnlineStore.API.csproj` — **lines 3–7** for `net10.0`, nullable, implicit usings; **lines 9–23** for the package list: `BCrypt.Net-Next` 4.2.0, `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.9, `Microsoft.AspNetCore.OpenApi` 10.0.9, `Microsoft.EntityFrameworkCore.Design`/`.Tools` 10.0.9, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, `Swashbuckle.AspNetCore` 10.2.3.
3. `OnlineStore.API/appsettings.json` — **lines 2–4** connection string (note `Port=5433`, `Database=OnlineStoreDb`, `Username=postgres`) and **lines 5–9** the `Jwt` section (`Key`, `Issuer` = `OnlineStore.API`, `Audience` = `OnlineStore.Client`). **Read it to learn the shape only — see the secrets rule in Edge Cases before writing anything from it.**
4. `OnlineStore.API/Properties/launchSettings.json` — **lines 4–13** the `http` profile (`http://localhost:5016`) and **lines 14–23** the `https` profile (`https://localhost:7225;http://localhost:5016`). Both set `ASPNETCORE_ENVIRONMENT=Development` and `launchUrl: "swagger"`.

**Backend — API surface.** Read each controller's attributes and return statements:

5. `OnlineStore.API/Controllers/AuthController.cs` — 95 lines, read all. `[Route("api/auth")]` **line 13**; `register` **lines 25–58** (409 on duplicate email, including the `DbUpdateException` race path **lines 49–53**); `login` **lines 60–81** (one generic 401 for both unknown email and wrong password, **lines 67–72**); `me` **lines 83–94** with the comment on **lines 87–88** explaining that inbound `sub`/`email` are read via `ClaimTypes`, not `JwtRegisteredClaimNames`. **Critically: `Register` never assigns `Role` (lines 36–41)** — see `Entities/User.cs`.
6. `OnlineStore.API/Controllers/ProductsController.cs` — `[Authorize(Roles = "admin")]` at class level **line 13**, with `[AllowAnonymous]` on `List` **line 25** and `GetById` **line 79**. `List` query parameters and clamping **lines 27–33** (`search`, `categoryId`, `brandId`, `page = 1`, `pageSize = 20`; `page` floored at 1, `pageSize` clamped to 1–100); the **non-admin callers see only `IsActive` products** rule **lines 41–45**; `ILike` case-insensitive name search **line 57**; response shape `{ items, page, pageSize, total }` **line 71**. `Create` **lines 105–107** is `[Consumes("multipart/form-data")]`; `Update` **lines 158–160** likewise. `Delete` **lines 223–246** — note it captures `ImageUrl` before removal and deletes the file only after the row is gone (**lines 235–241**).
7. `OnlineStore.API/Controllers/CartController.cs` — class-level `[Authorize]` **line 13**, `[Route("api/cart")]` **line 12**; `Add` **lines 23–74**, `Get` **lines 77–107**, `Delete` **lines 110–141**. Note `Forbid()` on **line 130** when the cart item belongs to another user.
8. `OnlineStore.API/Controllers/OrdersController.cs` — class-level `[Authorize]` **line 13**. Read `Checkout` **lines 23–97** carefully: all lines are validated **before** any write (**lines 45–57**), then a transaction (**line 59**), name/price snapshotting into `OrderItem` (**lines 62–75**), stock decrement (**lines 77–81**), `await Task.Delay(1500)` simulating a payment gateway with `Status = "paid"` always succeeding (**lines 85–86**), and cart clearing (**line 88**). `GetById` returns `Forbid()` for another user's order (**line 149**).
9. `OnlineStore.API/Controllers/CategoriesController.cs` and `OnlineStore.API/Controllers/BrandsController.cs` — identical shape: class-level `[Authorize(Roles = "admin")]` **line 12**, `[AllowAnonymous]` on `List` **line 22**. In `CategoriesController`, note the duplicate-name `BadRequest` (**line 54**) and the in-use `Conflict` on delete (**line 117**).

**Backend — domain and services**

10. `OnlineStore.API/Data/AppDbContext.cs` — 71 lines, read all. Seven `DbSet`s **lines 12–18**; unique index on `User.Email` **lines 23–25**; `Product → Category`/`Brand` with `DeleteBehavior.Restrict` **lines 27–37**; `CartItem` cascades **lines 39–49**; `Order → User` cascade **lines 51–55**; `OrderItem → Order` cascade **lines 57–61**; and **`OrderItem → Product` as `Restrict` with the explanatory comment on lines 63–69** ("preserve order history … Retire via `IsActive = false`").
11. `OnlineStore.API/Entities/User.cs` — **`Role` defaults to `"customer"`**. This plus fact 5 is why there is no way to create an admin through the API.
12. `OnlineStore.API/Entities/Order.cs` — the `Status` comment documents the allowed values `"pending" | "paid" | "failed"`. `OnlineStore.API/Entities/OrderItem.cs` — `ProductName`/`UnitPrice` are commented "snapshot at checkout".
13. `OnlineStore.API/Services/TokenService.cs` — claims built **lines 29–34** (`sub` = user id, `email`, `ClaimTypes.Role`), `expires: DateTime.UtcNow.AddDays(7)` **line 40**, `HmacSha256` **line 27**.
14. `OnlineStore.API/Services/ImageStorageService.cs` — allowed extensions `.jpg .jpeg .png .webp` **line 24**, `MaxBytes = 5 * 1024 * 1024` **line 25**, target `images/products` **line 26**, GUID filenames **line 56**, returned public URL shape `/images/products/{guid}{ext}` **line 62**. `InvalidImageException` **lines 17–20** maps to 400 at the controller.
15. `OnlineStore.API/Migrations/` — list the directory. Six migrations in order: `InitialCreate`, `AddUser`, `AddCategoryBrandAndProductFks`, `AddCartItem`, `AddProductImageUrl`, `AddOrders`.
16. All files in `OnlineStore.API/Dtos/` — small records; read them to transcribe request/response shapes and validation attributes exactly. Note `RegisterRequest` requires **min 8 chars plus one uppercase and one digit** (regex), `CreateProductFormRequest.Image` is `[Required]` while `UpdateProductFormRequest.Image` is **nullable** and it alone carries `IsActive`.

**Frontend**

17. `online-store-frontend/nuxt.config.ts` — 12 lines. `compatibilityDate: "2025-07-15"`, `modules: ["@nuxtjs/tailwindcss", "@pinia/nuxt"]`, `css: ["vue-sonner/style.css"]`, and **`runtimeConfig.public.apiBase = "https://localhost:7225/api"` on line 9** — which pairs with the backend's **`https`** launch profile, not `http`.
18. `online-store-frontend/package.json` — scripts `build`/`dev`/`generate`/`preview`/`postinstall`; dependencies Nuxt `^4.5.0`, Vue `^3.5.40`, Pinia `^4.0.2`, `@pinia/nuxt` `^1.0.1`, `@nuxtjs/tailwindcss` 6.14.0, `vue-sonner` `^2.0.9`, `zod` `^4.4.3`, `vue-router` `^5.2.0`. **There is no `test` script** — say plainly that no test runner is configured.
19. `online-store-frontend/README.md` — the current file is the unmodified Nuxt starter template (75 lines, four package managers, no project-specific content). This is the file being **replaced**.
20. `online-store-frontend/app/` — walk the tree and describe each directory: `middleware/` (`admin.ts`, `auth.ts`, `guest.ts`, `role-area.global.ts`), `layouts/` (`default.vue`, `admin.vue`), `stores/auth.ts`, `plugins/auth.ts`, `composables/` (`useApi`, `useBrands`, `useCart`, `useCategories`, `useOrders`, `useProductImage`, `useProducts`), `components/base/` (`Button`, `Form`, `Input`, `Select`), `components/admin/` (`StatCard`, `RecentProductsTable`), `utils/` (`format.ts`, `validation.ts`), and the `pages/` route map.
21. `online-store-frontend/app/composables/useProducts.ts` — **`server: false` on lines 44 and 55, with the comment explaining that Nitro's SSR fetch rejects the .NET dev server's self-signed HTTPS certificate.** This is the single most confusing thing a newcomer hits; the setup doc must explain it.
22. `online-store-frontend/app/composables/useProductImage.ts` — 11 lines. It strips a trailing `/api` from `apiBase` to build the image origin, because images are served from the API host's `wwwroot`, not under `/api`.
23. `online-store-frontend/app/stores/auth.ts` — the `auth` cookie (`maxAge` 7 days, `sameSite: "lax"`, `path: "/"`) holding `{ token, user }` **lines 15–21**; getters **lines 26–29**; `login`/`logout`/`loadFromStorage` **lines 31–72**, including the `localStorage` mirror.
24. `online-store-frontend/app/plugins/auth.ts` — 6 lines, **universal** (not `.client`), with the comment explaining it must run on the server so guards resolve during SSR.
25. `online-store-frontend/app/utils/validation.ts` — zod schemas. **`registerSchema` requires only `min(6)` for password (line 17)**, which is weaker than the backend's rule (fact 16) — see Edge Cases.
26. `online-store-frontend/tailwind.config.ts` — the `primary` palette with brand `#1C3684`, plus the follow-up comment on **lines 5–8** noting hard-coded hex literals still in the auth pages.

**Repository level**

27. Root layout: `.claude/`, `.github/` (**contains no files**), `.gitignore`, `.squad/`, `OnlineStore.API/`, `online-store-frontend/`. **No root `README.md` exists** and there is no root `package.json` or solution-wide build script — the two apps are built independently.
28. Root `.gitignore` — covers `.vs/`, `bin/`, `obj/`, `node_modules/`, `.env*`, plus a squad-kit-managed block. Note it does **not** ignore `appsettings.json`.
29. Three separate squad-kit workspaces: `.squad/` (root, `projectRoots: ["."]`), `online-store-frontend/.squad/`, `OnlineStore.API/.squad/`. Each has its own `config.yaml`, `plans/00-index.md` and `stories/`.
30. Verified local tool versions to quote as *known-good*, not as minimums: Node **v24.13.0**, pnpm **10.28.0**, .NET SDK **10.0.400**. Git remote is `https://github.com/ebtsamali/online-store`.

---

## Implementation tasks

Write plain GitHub-flavoured Markdown. Every command block must state the directory it runs in. Use relative links between documents so they work on GitHub. **Do not** invent behaviour: if something is not in the Context list above, either read the source and confirm it, or leave it out.

### 1 — `README.md` (repo root)

**Create file: `README.md`**

Sections, in order:

1. **Title and one-paragraph summary** — an online store split into an ASP.NET Core REST API and a Nuxt 4 storefront with an admin area.
2. **Architecture** — a table of the two apps: name, path, stack, dev URL. Backend `OnlineStore.API` → .NET 10 / EF Core 10 / PostgreSQL → `https://localhost:7225` (+ `http://localhost:5016`). Frontend `online-store-frontend` → Nuxt 4 / Vue 3 / Pinia / Tailwind → `http://localhost:3000`. State explicitly that there is **no root build script and no solution-wide package manager** — each app is built in its own directory.
3. **How the two halves connect** — a short list: the frontend reads `runtimeConfig.public.apiBase`; the backend's CORS policy admits only `http://localhost:3000`; JWTs travel in the `Authorization: Bearer` header; product images are served as static files from the API host.
4. **Prerequisites** — .NET SDK 10, Node.js 20+ (verified working on v24.13.0), pnpm (verified on 10.28.0), PostgreSQL, and `dotnet-ef` (note it ships as a global tool: `dotnet tool install --global dotnet-ef`). Present the verified versions as "known good", not as hard minimums.
5. **Quick start** — two fenced blocks, backend first (restore → configure connection string → `dotnet ef database update` → `dotnet run --launch-profile https`), then frontend (`pnpm install` → `pnpm dev`). Finish with "open http://localhost:3000". Link to `docs/local-setup.md` for the detail.
6. **Creating the first admin** — state that registration always creates a `customer` and that promotion is a manual SQL step; show the statement (task 7 owns the canonical copy) and link to `docs/auth-and-roles.md`.
7. **Repository layout** — a tree of the top level with one line per entry, including all three `.squad/` workspaces.
8. **Documentation index** — a table linking `docs/local-setup.md`, `docs/api-reference.md`, `docs/data-model.md`, `docs/auth-and-roles.md`, `online-store-frontend/README.md`, `OnlineStore.API/README.md`.
9. **Project conventions** — how squad-kit is used here: intakes under `.squad/stories/<feature>/<story>/intake.md`, plans under `.squad/plans/<feature>/NN-story-<slug>.md`, one index per workspace. Mention that plans are the design record and are worth reading before changing a feature.
10. **Testing and CI** — one honest paragraph: **no test runner is configured in either app** (no `test` script in `package.json`, no test project in the solution) and **there is no CI pipeline** (`.github/` holds no workflow files). Say what a contributor should do instead: the manual verification steps in the relevant plan, plus `pnpm build` and `dotnet build`.

### 2 — `online-store-frontend/README.md`

**Replace the entire existing file.** None of the current starter content survives.

1. **What this app is** — the storefront plus the `/admin` area; Nuxt 4 with SSR enabled.
2. **Requirements and install** — Node/pnpm; `pnpm install`; note `postinstall` runs `nuxt prepare`.
3. **Scripts** — a table of the four scripts from `package.json` with what each does and the port `pnpm dev` binds (3000). Add a row-level note that **the port matters**: the backend's CORS policy allows only `http://localhost:3000`, so running on another port breaks every API call.
4. **Configuring the API base URL** — `runtimeConfig.public.apiBase` and its current value; that `useApi()` is the only accessor; and that it can be overridden at runtime with the `NUXT_PUBLIC_API_BASE` environment variable (Nuxt's standard `runtimeConfig` override — state the variable name, and verify it before asserting anything further).
5. **Project structure** — a table over `app/`, one row per directory from Context 20, saying what belongs there.
6. **Routing and layouts** — the page map from `app/pages/`, which layout each area uses (`default.vue` for the storefront, `admin.vue` for `/admin/*`, `layout: false` for the three auth pages), and how `definePageMeta` assigns both layout and middleware.
7. **Route protection** — a short table of the four middleware files and what each does, then link to `docs/auth-and-roles.md` for the full rules. **Keep this consistent with the current implementation**: `role-area.global.ts` is global and enforces mutually exclusive admin/customer areas; `admin.ts` and `auth.ts` own the logged-out redirects; `guest.ts` keeps signed-in users off the login and register pages.
8. **State and persistence** — the Pinia `auth` store, the `auth` cookie, the `localStorage` mirror, and why the hydration plugin is universal rather than client-only.
9. **Data fetching** — the composables table; that they wrap `useFetch`; the distinct-`key` requirement noted in `useProducts.ts`; and **the `server: false` caveat with its reason** (self-signed dev certificate rejected by Nitro during SSR), pointing to `docs/local-setup.md` for the fix.
10. **Forms, validation and toasts** — the `Base*` components, zod schemas in `app/utils/validation.ts`, `fieldErrors()` flattening one message per field, and `vue-sonner` for toasts.
11. **Styling** — Tailwind via the Nuxt module, the `primary` palette and brand hex, and the outstanding hard-coded-hex cleanup noted in `tailwind.config.ts`.
12. **Building** — `pnpm build` then `pnpm preview`; note the output lands in `.output/` and can be run with `node .output/server/index.mjs`.
13. **Testing** — state plainly that no test runner is configured, and that verification today is the manual steps in the relevant `.squad/plans/` story plus a clean `pnpm build`.

### 3 — `OnlineStore.API/README.md`

**Create file: `OnlineStore.API/README.md`**

1. **What this app is** — a REST API over PostgreSQL issuing JWTs, serving product images as static files.
2. **Stack** — `net10.0`, EF Core 10 + Npgsql, BCrypt for password hashing, JWT bearer auth, Swashbuckle for Swagger. Include the package table from Context 2 with versions.
3. **Requirements** — .NET SDK 10, PostgreSQL, `dotnet-ef`.
4. **Configuration** — a table of every key: `ConnectionStrings:DefaultConnection`, `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Logging`, `AllowedHosts`. Give the **shape** of the connection string with placeholders (`Host=…;Port=…;Database=…;Username=…;Password=…`), state the values that are not secret (port **5433**, database `OnlineStoreDb`, issuer `OnlineStore.API`, audience `OnlineStore.Client`), and note `Jwt:Key` must be at least 32 bytes for HMAC-SHA256. **Follow the secrets rule in Edge Cases — do not transcribe the committed password or key.**
5. **Running** — the two launch profiles as a table (profile, URLs, environment), the `dotnet run --launch-profile <name>` command, and that both open Swagger at `/swagger`. **Call out that the frontend's default `apiBase` points at `https://localhost:7225`, so the `https` profile is the one that works out of the box.**
6. **Swagger** — available in Development only (`Program.cs` lines 75–80); how to authorize in the UI (paste the raw JWT, no `Bearer ` prefix — quote the description string from `Program.cs` line 30).
7. **Database and migrations** — `dotnet ef database update` to apply, `dotnet ef migrations add <Name>` to create, `dotnet ef migrations list`; the six existing migrations in order; and that the schema is managed exclusively by EF migrations.
8. **Project structure** — a table over `Controllers/`, `Dtos/`, `Entities/`, `Data/`, `Services/`, `Migrations/`, `wwwroot/`.
9. **Request pipeline** — the ordered middleware from `Program.cs` lines 74–95 with one line each, noting the two environment-conditional pieces (Swagger in Development, HTTPS redirection outside it).
10. **Cross-cutting conventions** — every controller wraps its body in `try/catch` returning `500 { "message": "Something went wrong" }`; validation comes from data annotations on the DTOs, so `[ApiController]` produces `400` with a `ValidationProblemDetails` body automatically; auth failures are `401`, ownership failures `403`.
11. **Image uploads** — the rules from Context 14 and that files land in `wwwroot/images/products/` under GUID names, served at `/images/products/{file}`.
12. **Links** — to `docs/api-reference.md`, `docs/data-model.md`, `docs/auth-and-roles.md`, and this project's own plans in `.squad/plans/`.
13. **Testing** — no test project exists; verification is `dotnet build` plus the manual steps in the relevant plan.

### 4 — `docs/local-setup.md`

**Create file: `docs/local-setup.md`**

The document that gets someone from a fresh clone to a working pair of apps.

1. **Prerequisites with verified versions** (Context 30), and how to check each (`dotnet --version`, `node -v`, `pnpm -v`, `psql --version`).
2. **PostgreSQL** — create the database; **highlight that the configured port is `5433`, not the default `5432`**, so a stock local install will not be found without either changing the port or editing the connection string. Give both options.
3. **Backend configuration** — where settings live, and the recommended way to hold secrets locally: `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "…"` and `dotnet user-secrets set "Jwt:Key" "…"` run in `OnlineStore.API/`, or the `ConnectionStrings__DefaultConnection` / `Jwt__Key` environment-variable form. Explain the precedence order (env vars and user secrets override `appsettings.json`).
4. **Apply migrations** — `dotnet ef database update` in `OnlineStore.API/`, with the "command not found" remedy (`dotnet tool install --global dotnet-ef`).
5. **Run the backend** — `dotnet run --launch-profile https` and what to expect (Swagger opens).
6. **Run the frontend** — `pnpm install`, `pnpm dev`, expect `http://localhost:3000`.
7. **The port/profile matrix** — a table making the pairing explicit: frontend `apiBase` value ↔ required launch profile ↔ what breaks if they disagree. Cover both directions: keep `https://localhost:7225/api` and run the `https` profile, or switch `apiBase` to `http://localhost:5016/api` and run `http`.
8. **The self-signed certificate problem** — the most important troubleshooting section. Explain: the .NET dev server uses a self-signed cert; Nitro's server-side fetch rejects it; this is why `useProducts.ts` sets `server: false`; the browser will also warn on first visit to `https://localhost:7225`. Remedies, in order: run `dotnet dev-certs https --trust`; visit the API origin once and accept the certificate; or use the `http` profile with a matching `apiBase`.
9. **CORS** — only `http://localhost:3000` is allowed; the symptom when the frontend runs elsewhere (browser CORS error, request never reaches a controller) and the fix (`Program.cs` lines 42–48).
10. **Create an admin** — the manual SQL promotion (canonical copy lives in task 7; link there and repeat the one statement for convenience).
11. **Troubleshooting table** — symptom → cause → fix. At minimum: connection refused on 5433; `dotnet ef` not found; 401 on every admin call (logged in as customer, or a stale role in the cookie needing re-login); CORS error; certificate error during SSR; images 404 (API not running, or `apiBase` missing the `/api` suffix that `useProductImage` strips); `pnpm dev` port already in use.

### 5 — `docs/api-reference.md`

**Create file: `docs/api-reference.md`**

1. **Conventions** — base path `/api`; JSON in and out except the two multipart product endpoints; `Authorization: Bearer <token>`; the shared error envelope `{ "message": "…" }`; automatic `400` validation responses from `[ApiController]`; the blanket `500 { "message": "Something went wrong" }`.
2. **Authorization legend** — three levels used throughout the table: **Anonymous**, **Authenticated** (`[Authorize]`), **Admin** (`[Authorize(Roles = "admin")]`).
3. **Endpoint index** — one table, all 22 endpoints, columns: method, path, auth level, purpose. Derive strictly from Context 5–9.
4. **Per-endpoint detail** — one subsection each, grouped by controller (Auth, Products, Categories, Brands, Cart, Orders). For each: verb and path, auth level, request (query/route/body/form fields with types and validation rules from the DTOs), success response with a JSON example, and an error table of every status code that endpoint actually returns with its message string. Specific things that must appear:
   - `GET /api/products` — all five query parameters, the `page`/`pageSize` clamping, the `{ items, page, pageSize, total }` envelope, case-insensitive partial name search, and **that anonymous and non-admin callers receive only `IsActive` products while an admin sees all**.
   - `POST`/`PUT /api/products` — `multipart/form-data`, field lists, `Image` **required on create, optional on update**, `IsActive` present **only** on update, and the image validation rules.
   - `DELETE /api/products/{id}` — note the gotcha from Edge Cases about products referenced by an order.
   - `POST /api/auth/register` — the password policy (min 8, one uppercase, one digit), the `201` body, the `409` on duplicate email, and that the created user is always role `customer`.
   - `POST /api/auth/login` — the `AuthResponse` shape `{ token, name, email, role }` and the deliberately generic `401`.
   - `GET /api/auth/me` — the `{ id, email, role }` shape read from claims.
   - `POST /api/orders/checkout` — no request body; the pre-validation-then-transaction order; snapshotting; stock decrement; the simulated 1.5-second payment that always succeeds; the cart being emptied; and every `400` message.
   - `DELETE /api/cart/{id}` and `GET /api/orders/{id}` — the `403` when the row belongs to another user.
   - Categories/Brands — the duplicate-name `400` and the in-use `409` on delete.

### 6 — `docs/data-model.md`

**Create file: `docs/data-model.md`**

1. **Overview** — EF Core code-first against PostgreSQL; `AppDbContext` is the single model definition; schema changes only ever land as migrations.
2. **Entity tables** — one table per entity (`User`, `Product`, `Category`, `Brand`, `CartItem`, `Order`, `OrderItem`) with column, CLR type, default, and notes. Include the defaults that carry meaning: `User.Role = "customer"`, `Product.IsActive = true`, `CartItem.Quantity = 1`, `Order.Status = "pending"`, and the `CreatedAt = DateTime.UtcNow` defaults.
3. **Relationships and delete behaviour** — a table of every configured relationship with its `DeleteBehavior`, citing `AppDbContext.cs` line ranges. Give the **`OrderItem → Product` `Restrict`** rule its own callout with the reasoning quoted from lines 63–64: order history is preserved, so a product that appears in any order cannot be hard-deleted — retire it with `IsActive = false` instead.
4. **Constraints** — the unique index on `User.Email` and the 409/`DbUpdateException` path it produces on concurrent registration.
5. **Order lifecycle** — `pending → paid` in the current simulation, with `failed` declared in the entity comment but never written by the code. Say that explicitly rather than implying failure handling exists.
6. **Snapshotting** — why `OrderItem` stores `ProductName` and `UnitPrice` rather than joining, and what that means for later price changes.
7. **Migrations** — the six migrations in order with a line on what each added, plus the commands to add and apply. Note `AppDbContextModelSnapshot.cs` is generated and must not be hand-edited.
8. **Image storage** — that `Product.ImageUrl` holds a site-relative path, not a filesystem path; where files live; the GUID naming; the allowed types and 5 MB cap; and that deleting a product removes its file only after the row is deleted.

### 7 — `docs/auth-and-roles.md`

**Create file: `docs/auth-and-roles.md`**

1. **The two roles** — `customer` and `admin`, the exact lowercase strings compared in both halves.
2. **Registration and login flow** — numbered walkthrough: `POST /api/auth/register` (BCrypt hash, role forced to `customer`) → `POST /api/auth/login` → `AuthResponse` → the frontend stores it.
3. **Creating an admin** — the canonical instruction for the whole repo. Registration cannot produce one; promote with SQL, then **log in again** because the role is baked into the JWT and into the frontend cookie at login:
   ```sql
   UPDATE "Users" SET "Role" = 'admin' WHERE "Email" = 'you@example.com';
   ```
   Note the quoted PascalCase identifiers are required by PostgreSQL for EF's default naming.
4. **The JWT** — claims (`sub` = user id, `email`, role), 7-day lifetime, HMAC-SHA256, issuer/audience, and the validation parameters the API enforces. Include the `Me()` note: because the JWT handler remaps inbound `sub`→`NameIdentifier` and `email`→`Email`, server code must read them via `ClaimTypes`.
5. **Backend enforcement** — an authorization matrix by controller: which are `[Authorize(Roles = "admin")]` at class level with `[AllowAnonymous]` exceptions, which are `[Authorize]`, which are open. State that **this is the only real security boundary.**
6. **Frontend session storage** — the `auth` cookie (7 days, `sameSite: "lax"`, `path: "/"`) holding token plus user, the `localStorage` mirror, and the universal plugin that hydrates the store before middleware so guards resolve correctly during SSR. Explain why a client-only plugin would break a direct navigation.
7. **Frontend route protection** — a table of the four middleware files: `role-area.global.ts` (global; admin confined to `/admin/*`, customer excluded from it; ignores logged-out visitors), `admin.ts` (logged-out → `/admin/login`; authenticated non-admin → `/`), `auth.ts` (logged-out → `/auth/login`), `guest.ts` (signed-in visitors off the three auth pages). Then a behaviour matrix: rows = logged out / customer / admin, columns = storefront, customer-account pages, `/admin/*`, auth pages; cells = renders or the redirect target. **This must match the current implementation — reconcile against the Story 16 plan linked in Prerequisites.**
8. **Logout** — role-aware: admins land on `/admin/login`, customers on `/auth/login`. Include the ordering constraint: state is cleared before navigating, because the guards would otherwise see a live session and bounce the user back.
9. **Frontend guards are not security** — a short, blunt section. They are UX. A user can edit the cookie or the store. The API still returns 401/403. Never move an authorization decision into the frontend.
10. **Known gaps** — the two verified items from Edge Cases below: the password-policy mismatch between client and server, and the role-string case sensitivity. Frame them as "known, not yet fixed", with the file and line for each.

---

## Edge Cases & Failure Modes

These are the traps a documentation pass can walk into, and the verified inconsistencies the docs must record rather than paper over.

- **Secrets must not be transcribed.** `OnlineStore.API/appsettings.json` **line 3** contains a real (if trivial) database password and **line 6** a JWT signing key. Both are committed to git and the root `.gitignore` does not exclude the file. **Every document must use placeholders** (`Password=<your-password>`, `"Key": "<32+ character secret>"`) and never the literal values. Add a short "Security note" in `OnlineStore.API/README.md` and `docs/local-setup.md`: these are development placeholders, they are in version control, and they must be replaced via user-secrets or environment variables and rotated before any deployment. **Actually changing the files, rotating the key, or amending `.gitignore` is out of scope for this story** — record it as a follow-up.
- **Password policy mismatch between the two halves (verified).** `online-store-frontend/app/utils/validation.ts` line 17 accepts any password of 6+ characters, while `OnlineStore.API/Dtos/RegisterRequest.cs` requires 8+ with at least one uppercase letter and one digit. A password such as `abcdef` passes client validation and is then rejected by the API with a `400`. Document this in `docs/auth-and-roles.md` §10 and in the register endpoint's error table; **do not fix it here**.
- **Deleting a product that appears in an order returns 500, not 409 (verified).** `OrderItem → Product` is `Restrict` (`AppDbContext.cs` lines 65–69), so the delete throws `DbUpdateException`, which `ProductsController.Delete`'s bare `catch` (lines 244–246) converts to `500 { "message": "Something went wrong" }`. `CategoriesController.Delete` handles the analogous case properly with a `409` (line 117). Document the actual behaviour and the `IsActive = false` workaround; flag the inconsistency as a known gap.
- **Role comparison is case-sensitive on both sides.** The backend compares `Roles = "admin"` and the frontend compares `role === "admin"` (`app/stores/auth.ts` line 28). A row promoted to `'Admin'` produces a user who is neither a working admin nor a normal customer. The SQL in task 7 must use lowercase `'admin'`, and the gotcha belongs in the troubleshooting table.
- **`apiBase` and the launch profile must agree.** The frontend default (`https://localhost:7225/api`) only matches the `https` profile. Running `--launch-profile http` while leaving `apiBase` untouched produces connection failures with no CORS error to explain them. This pairing gets its own table in `docs/local-setup.md` §7.
- **The frontend port is load-bearing.** CORS admits only `http://localhost:3000`. If port 3000 is taken, Nuxt picks another port and every API call fails in the browser with a CORS error while the API log shows nothing. Both the frontend README and the troubleshooting table must say so.
- **`server: false` is a workaround, not a design choice.** Document the reason (self-signed cert rejected by Nitro during SSR) alongside the remedy, so nobody "fixes" it by removing the flag and then sees SSR fetch failures. Source: `useProducts.ts` lines 42–44 and 53–55.
- **Do not describe tests or CI that do not exist.** No `test` script in `package.json`, no test project in `OnlineStore.API.slnx`, no files in `.github/`. Documentation that implies otherwise is worse than silence. Every "Testing" section says what is actually true and points at `pnpm build` / `dotnet build` plus the manual steps in the relevant plan.
- **The stated versions are observed, not required.** Node v24.13.0, pnpm 10.28.0 and .NET SDK 10.0.400 are what this machine has. `csproj` pins `net10.0`, which genuinely requires the .NET 10 SDK; nothing in the repo pins a Node or pnpm version (no `.nvmrc`, no `engines` field, and `pnpm-workspace.yaml` contains only an `allowBuilds` entry). Present them as "verified working", not as minimums.
- **Three `.squad` workspaces are easy to confuse.** Each has an independent `NN` sequence: the frontend runs 01–16, the backend 01–08, and this root workspace starts at 01. Say this in the root README's conventions section so nobody reads "Story 01" as ambiguous.
- **Docs go stale.** Every claim in these documents is tied to a file that can change. Where a document states a rule that lives in one place (ports, the CORS origin, the JWT lifetime, the image size cap), cite the file so the next reader can check it. Prefer citing a file and symbol over a bare line number, which drifts fastest.
- **Relative links must resolve on GitHub.** From `docs/*.md`, the apps are `../online-store-frontend/` and `../OnlineStore.API/`; from the root README, `docs/…` and `online-store-frontend/…`. Verify every link by clicking through after the files exist.

---

## Test Plan

No automated test runner exists in either app, and documentation is not unit-testable. Verification is a review pass plus link and command checking.

1. **Command check (manual, required):** execute every command block in `README.md` and `docs/local-setup.md` in order on the current checkout, from the stated directory. Any command that does not run as written is a defect in the doc.
2. **Link check:** confirm every relative link resolves — the seven new documents, the three `.squad/plans/` references, and the cross-links between `docs/*`. A quick sweep: `grep -oE '\]\([^)]+\)' README.md docs/*.md online-store-frontend/README.md OnlineStore.API/README.md` and confirm each target path exists.
3. **Endpoint coverage check:** cross-check `docs/api-reference.md` against the controllers — `grep -n -E '\[Http(Get|Post|Put|Delete)' OnlineStore.API/Controllers/*.cs` must yield exactly the endpoints documented, with no extras and none missing (22 total: Auth 3, Products 5, Categories 4, Brands 4, Cart 3, Orders 3).
4. **Secret-leak check (required):** confirm neither the committed database password nor the JWT key string appears anywhere in the new files. `grep -rn -e 'Password=000' -e 'CHANGE_ME_dev_only' README.md docs/ online-store-frontend/README.md OnlineStore.API/README.md` must return **nothing**.
5. **Consistency check:** the route-protection tables in `online-store-frontend/README.md` §7 and `docs/auth-and-roles.md` §7 must agree with each other and with the four files in `online-store-frontend/app/middleware/`.
6. **Fresh-reader review:** have someone who has not worked on the repo follow `docs/local-setup.md` end to end on a clean machine and note every point where they had to guess. Each such point is a doc bug.
7. **Regression:** `git status` must show only the seven documentation files as added/modified — **no application source, config, or `.gitignore` changes**.

---

## Verification Steps

1. **Files exist:** `README.md`, `docs/local-setup.md`, `docs/api-reference.md`, `docs/data-model.md`, `docs/auth-and-roles.md`, `online-store-frontend/README.md`, `OnlineStore.API/README.md`.
2. **Frontend README replaced:** `grep -c "Nuxt Minimal Starter" online-store-frontend/README.md` returns `0`.
3. **Backend builds:** `dotnet build` in `OnlineStore.API/` — clean (confirms the docs pass introduced no source change).
4. **Frontend builds:** `pnpm build` in `online-store-frontend/` — clean, for the same reason.
5. **Quick start works from the docs alone:** following only `README.md` §5 and `docs/local-setup.md`, bring up the API and the frontend and load `http://localhost:3000` with products rendering.
6. **Admin promotion works as documented:** register a user, run the SQL from `docs/auth-and-roles.md` §3, log in again at `/admin/login`, reach `/admin/dashboard`.
7. **Swagger matches the reference:** open `/swagger` on the running API and spot-check five endpoints against `docs/api-reference.md` — paths, verbs, auth padlocks, and request shapes must agree.
8. **Secret-leak check:** the `grep` from Test Plan step 4 returns nothing.
9. **Link check:** the sweep from Test Plan step 2 shows no broken targets.
10. **Regression:** `git status --short` lists only the seven documentation files.

---

## Done Criteria

- [ ] `README.md` exists at the repo root and covers summary, architecture, how the halves connect, prerequisites, quick start, admin creation, layout, a documentation index, conventions, and the honest testing/CI statement.
- [ ] `online-store-frontend/README.md` no longer contains any Nuxt-starter boilerplate and documents scripts, `apiBase`, structure, routing, the four middleware, state/persistence, composables (including `server: false` and its reason), forms/validation, styling, and building.
- [ ] `OnlineStore.API/README.md` documents stack and package versions, every configuration key with placeholder values, both launch profiles, Swagger, migrations, structure, the request pipeline, error conventions, and image upload rules.
- [ ] `docs/local-setup.md` gets a fresh clone running, and covers the **5433** DB port, user-secrets/env-var configuration, the `apiBase` ↔ launch-profile matrix, the self-signed certificate problem, the CORS origin, admin promotion, and a troubleshooting table.
- [ ] `docs/api-reference.md` documents **all 22** endpoints with auth level, request shape, response shape, and every status code each one actually returns.
- [ ] `docs/data-model.md` documents all seven entities, every relationship with its delete behaviour, the `OrderItem → Product` `Restrict` rule and its rationale, the unique email index, the order lifecycle, snapshotting, all six migrations, and image storage.
- [ ] `docs/auth-and-roles.md` documents both roles, the login flow, admin promotion by SQL with the re-login requirement, JWT claims and lifetime, the backend authorization matrix, frontend session storage, the four-middleware behaviour matrix, role-aware logout with its ordering constraint, and an explicit "frontend guards are not security" section.
- [ ] The three verified inconsistencies are recorded as known gaps, each with file and line: the password-policy mismatch, the product-delete `500`, and role-string case sensitivity.
- [ ] No document contains the committed database password or JWT key; all secret values are placeholders, and the security note with remediation is present.
- [ ] No document claims tests or CI exist.
- [ ] Every relative link resolves; every command block states its directory and runs as written.
- [ ] `git status` shows only documentation files changed — no application source, config, or `.gitignore` edits.
- [ ] `dotnet build` and `pnpm build` are both clean.

---

**Feature complete. Report results to the user with the acceptance-criteria checklist filled in.**
