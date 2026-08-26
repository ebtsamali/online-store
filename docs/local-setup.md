# Local setup

Getting a fresh clone of this repository running end to end. Follow the sections in order; the [troubleshooting table](#troubleshooting) at the end covers every failure this project is known to produce on a first run.

---

## 1. Prerequisites

| Tool | Required | Verified working | Check with |
|---|---|---|---|
| .NET SDK | 10.x | 10.0.400 | `dotnet --version` |
| Node.js | 20+ | v24.13.0 | `node -v` |
| pnpm | recent | 10.28.0 | `pnpm -v` |
| PostgreSQL | 14+ | — | `psql --version` |
| `dotnet-ef` | — | — | `dotnet ef --version` |

The .NET 10 SDK is non-negotiable: `OnlineStore.API.csproj` targets `net10.0` and an older SDK will not restore it. The Node and pnpm versions are what the project has been run with, not enforced minimums — nothing in the repository pins them.

If `dotnet ef` is missing:

```bash
dotnet tool install --global dotnet-ef
```

---

## 2. PostgreSQL

Create the database:

```sql
CREATE DATABASE "OnlineStoreDb";
```

> **The configured port is `5433`, not the PostgreSQL default `5432`.** A stock local installation listens on 5432 and will simply not be found.

Pick one of two fixes:

- **Point the API at your port** — set the connection string to `Port=5432` (next section). Simplest, and the usual choice.
- **Make PostgreSQL listen on 5433** — change `port = 5433` in `postgresql.conf` and restart. Do this if you already run another PostgreSQL instance on 5432.

Confirm you can connect before continuing:

```bash
psql -h localhost -p 5433 -U postgres -d OnlineStoreDb -c "SELECT 1;"
```

---

## 3. Configure the API

Settings resolve in this order, with later sources overriding earlier ones:

```
appsettings.json  →  appsettings.Development.json  →  user secrets  →  environment variables
```

`appsettings.json` is **committed to version control** and holds development placeholders only. Keep your real values out of it — use user secrets:

```bash
# run in OnlineStore.API/
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=OnlineStoreDb;Username=postgres;Password=<your-password>"
dotnet user-secrets set "Jwt:Key" "<a-secret-of-at-least-32-characters>"
```

Or environment variables, where `:` becomes `__`:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=OnlineStoreDb;Username=postgres;Password=<your-password>"
export Jwt__Key="<a-secret-of-at-least-32-characters>"
```

`Jwt:Key` must be **at least 32 characters** — HMAC-SHA256 needs a 256-bit key, and a shorter one throws at startup. Leave `Jwt:Issuer` (`OnlineStore.API`) and `Jwt:Audience` (`OnlineStore.Client`) alone unless you intend to invalidate every existing token; both are validated on every request.

---

## 4. Apply migrations

```bash
cd OnlineStore.API
dotnet ef database update
```

This creates all seven tables. If it reports `Could not execute because the specified command or file was not found`, install the CLI tool (step 1). If it fails to connect, revisit step 2 — the connection string is wrong or PostgreSQL is not listening where you think.

---

## 5. Run the API

```bash
cd OnlineStore.API
dotnet run --launch-profile https
```

Expect Swagger to open at `https://localhost:7225/swagger`. Your browser will warn about the certificate on first visit — that is expected and addressed in [section 8](#8-the-self-signed-certificate).

---

## 6. Run the frontend

```bash
cd online-store-frontend
pnpm install
pnpm dev
```

Expect `http://localhost:3000`. Open it — the home page should render products from the API.

---

## 7. Ports and launch profiles must agree

This is the single most common configuration mistake, because the failure gives you almost nothing to go on.

| Frontend `apiBase` | Required API profile | If they disagree |
|---|---|---|
| `https://localhost:7225/api` **(default)** | `https` | Requests fail to connect. No CORS error, no API log entry — the port simply isn't listening. |
| `http://localhost:5016/api` | `http` **or** `https` | Works with either; the `https` profile also binds 5016. |

Two valid setups:

**A — keep the default (recommended).** Change nothing in the frontend and run:

```bash
dotnet run --launch-profile https
```

**B — avoid HTTPS entirely.** Run the `http` profile and override the frontend at startup:

```bash
# terminal 1
cd OnlineStore.API && dotnet run --launch-profile http

# terminal 2
cd online-store-frontend && NUXT_PUBLIC_API_BASE=http://localhost:5016/api pnpm dev
```

Option B sidesteps the certificate problem below entirely, which makes it a good choice if you keep hitting it.

---

## 8. The self-signed certificate

The .NET development server presents a **self-signed** HTTPS certificate. Two distinct symptoms follow.

**In the browser.** Visiting `https://localhost:7225` shows a security warning. Requests from the Nuxt page to the API can be blocked until the certificate is accepted for that origin.

**During server-side rendering.** Nitro's server-side `fetch` rejects the certificate outright. This is why the product composables opt out of SSR:

```ts
// app/composables/useProducts.ts
server: false,
```

That flag is a **workaround, not a design decision**. Removing it without first trusting the certificate reintroduces SSR fetch failures.

Fixes, in order of preference:

1. **Trust the development certificate** — the real fix:
   ```bash
   dotnet dev-certs https --trust
   ```
   Then restart both apps. On Windows and macOS this installs it into the OS trust store; on Linux trust has to be configured per browser/runtime.
2. **Accept it once in the browser** — open `https://localhost:7225/swagger` and click through the warning. Fixes the browser, not SSR.
3. **Use HTTP** — setup option B in section 7. Sidesteps certificates completely.

---

## 9. CORS

The API allows exactly one origin:

```csharp
policy.WithOrigins("http://localhost:3000")
```

So the frontend **must** run on port 3000. If that port is busy, Nuxt starts on 3001 (or higher) without complaint, and then every API call fails with a CORS error in the browser console while the API logs show nothing — the browser blocks the response before your code sees it.

Free port 3000 rather than accepting the fallback:

```bash
# Windows
netstat -ano | findstr :3000

# macOS / Linux
lsof -i :3000
```

Adding a second allowed origin means editing `Program.cs` — treat that as a code change, not local configuration.

---

## 10. Create an admin

Registration **always** creates a `customer`; no endpoint can create an administrator. Register normally, then promote the row:

```sql
UPDATE "Users" SET "Role" = 'admin' WHERE "Email" = 'you@example.com';
```

The double quotes are required — EF Core creates PascalCase identifiers, which PostgreSQL folds to lowercase unless quoted. The role string must be lowercase `'admin'`; it is compared exactly on both sides.

Then **log in again** at `http://localhost:3000/admin/login`. The role is captured into the JWT and the frontend cookie at login, so an existing session keeps its old role until you re-authenticate.

Details in [`auth-and-roles.md`](auth-and-roles.md).

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `Npgsql.NpgsqlException: Connection refused` on startup or `ef database update` | Nothing listening on the configured port (usually the 5433-vs-5432 mismatch) | Section 2 — align the port or the connection string |
| `dotnet ef` — "command or file was not found" | EF CLI tool not installed | `dotnet tool install --global dotnet-ef` |
| App throws at startup mentioning the signing key | `Jwt:Key` shorter than 32 characters, or unset | Set a longer secret (section 3) |
| Frontend loads but every request fails, browser shows a CORS error | Frontend is not on port 3000 | Section 9 — free port 3000 |
| Frontend loads, requests fail with no CORS error and no API log line | `apiBase` and launch profile disagree | Section 7 — match them |
| Certificate / `UNABLE_TO_VERIFY_LEAF_SIGNATURE` errors, or SSR fetch failures | Untrusted self-signed dev certificate | Section 8 — `dotnet dev-certs https --trust` |
| Product images show as broken | API not running, or `apiBase` missing its `/api` suffix | `useProductImage()` strips a trailing `/api` to find the image origin; without that suffix it builds the wrong URL |
| `401` on every admin call while logged in | Signed in as a `customer`, or promoted in SQL without re-logging-in | Section 10 — promote, then log in again |
| Redirected to `/` immediately after reaching `/admin/…` | Session's role is not exactly `admin` | Check the stored role; the comparison is case-sensitive |
| `pnpm dev` — port already in use | Another process on 3000 | Section 9 |
| `500 { "message": "Something went wrong" }` deleting a product | The product appears in an existing order and is FK-protected | Set `IsActive = false` instead — see [`data-model.md`](data-model.md) |
| Register succeeds client-side then fails with `400` | Client accepts 6-character passwords; the API requires 8+ with an uppercase letter and a digit | Use a compliant password — known gap, see [`auth-and-roles.md`](auth-and-roles.md#known-gaps) |
| Editor reports unknown Nuxt auto-imports | `.nuxt/` not generated | `pnpm install` (runs `nuxt prepare`) |

---

## Verifying the setup

A working environment should pass all of these:

1. `https://localhost:7225/swagger` loads.
2. `GET /api/products` returns `200` from Swagger without a token.
3. `http://localhost:3000` renders products, with images.
4. Register a user, log in — the header shows a logged-in state.
5. Promote that user to `admin`, log in again at `/admin/login` — `/admin/dashboard` renders with the sidebar.
6. As an admin, visiting `/` or `/products` redirects to `/admin/dashboard` (the areas are mutually exclusive by design — see [`auth-and-roles.md`](auth-and-roles.md)).
