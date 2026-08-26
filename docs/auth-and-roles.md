# Authentication and roles

How identity works across both halves of the system: how a JWT is issued, what the API enforces, what the frontend enforces, and — importantly — which of those two is actually security.

---

## The two roles

| Role | Who | Can reach |
|---|---|---|
| `customer` | Everyone who registers | Storefront, cart, checkout, own orders |
| `admin` | Manually promoted | The `/admin` area and all write endpoints |

Both are **lowercase literals, compared exactly**, in two places:

- Backend — `[Authorize(Roles = "admin")]`
- Frontend — `role === "admin"` in the auth store's `isAdmin` getter

There is no role hierarchy and no third role. A value of `"Admin"` or `"ADMIN"` matches neither comparison and produces a user who is effectively locked out of both areas.

---

## Registration and login

**1. Register** — `POST /api/auth/register`

The password is hashed with BCrypt; the plaintext is never stored. `Role` is **not** assigned by the controller, so the `User` entity default `"customer"` applies. No request body can change that, and **no endpoint anywhere creates an admin**.

The response carries no token — registration and login are separate steps.

**2. Log in** — `POST /api/auth/login`

On success the API returns:

```json
{ "token": "eyJhbGciOi…", "name": "Ada Lovelace", "email": "ada@example.com", "role": "customer" }
```

A failure returns the same `401 { "message": "Invalid email or password" }` whether the email is unknown or the password is wrong, so the endpoint cannot be used to discover which accounts exist.

**3. The frontend stores the session** — see [Frontend session storage](#frontend-session-storage).

---

## Creating an admin

Registration always produces a `customer`, so the first administrator is promoted directly in the database:

```sql
UPDATE "Users" SET "Role" = 'admin' WHERE "Email" = 'you@example.com';
```

Two details that catch people out:

- **The double quotes are required.** EF Core creates PascalCase identifiers (`Users`, `Role`, `Email`); PostgreSQL folds unquoted identifiers to lowercase and the statement fails without them.
- **The value must be lowercase `'admin'`** — the comparison is case-sensitive on both sides.

Then **log in again.** The role is captured into the JWT *and* into the frontend's session cookie at the moment of login, so an existing session keeps its old role until re-authentication. Nothing re-reads the database mid-session.

The same applies to demotion: revoking admin in SQL does not end an existing admin session. There is no token revocation — see [Known gaps](#known-gaps).

---

## The JWT

Issued by `TokenService`.

| Property | Value |
|---|---|
| Algorithm | HMAC-SHA256 |
| Signing key | `Jwt:Key` — must be ≥ 32 characters |
| Issuer | `Jwt:Issuer` — `OnlineStore.API` |
| Audience | `Jwt:Audience` — `OnlineStore.Client` |
| Lifetime | **7 days** from issue |

Claims:

| Claim | Contents |
|---|---|
| `sub` | The user's `Id` |
| `email` | The user's email |
| `role` | `"customer"` or `"admin"` |

The API validates issuer, audience, lifetime **and** signing key on every request, so changing `Jwt:Issuer`, `Jwt:Audience` or `Jwt:Key` invalidates every token already issued.

**Reading claims in server code.** The JWT bearer handler remaps inbound claim names, so `sub` arrives as `ClaimTypes.NameIdentifier` and `email` as `ClaimTypes.Email`. Server code must therefore read:

```csharp
var id    = User.FindFirstValue(ClaimTypes.NameIdentifier);  // not JwtRegisteredClaimNames.Sub
var email = User.FindFirstValue(ClaimTypes.Email);
var role  = User.FindFirstValue(ClaimTypes.Role);
```

Looking these up by their raw JWT names returns `null`. `AuthController.Me()` carries a comment to this effect; `GET /api/auth/me` is the quickest way to confirm what a token actually contains.

---

## Backend enforcement

**This is the only real security boundary in the system.**

| Controller | Class-level attribute | Exceptions |
|---|---|---|
| `AuthController` | none | `me` is `[Authorize]` |
| `ProductsController` | `[Authorize(Roles = "admin")]` | `[AllowAnonymous]` on `List` and `GetById` |
| `CategoriesController` | `[Authorize(Roles = "admin")]` | `[AllowAnonymous]` on `List` |
| `BrandsController` | `[Authorize(Roles = "admin")]` | `[AllowAnonymous]` on `List` |
| `CartController` | `[Authorize]` | — |
| `OrdersController` | `[Authorize]` | — |

The pattern is deliberate: catalogue controllers **deny by default** and open up only their read endpoints. A new action added to any of them is admin-only unless explicitly marked otherwise — the safe direction for a mistake.

**Ownership** is enforced separately from authentication. Cart and order rows are resolved from the token's `sub` claim, never from a client-supplied user id, and reading another user's row returns `403`:

- `DELETE /api/cart/{id}` — `403` if the cart item belongs to someone else
- `GET /api/orders/{id}` — `403` if the order belongs to someone else

Two role-adjacent behaviours worth knowing:

- `GET /api/products` returns **only active products** to anonymous and non-admin callers; an admin sees everything. The same URL yields different data depending on the token.
- `GET /api/products/{id}` returns `404` for an inactive product unless the caller is an admin.

---

## Frontend session storage

`app/stores/auth.ts` holds `token` and `user` (`name`, `email`, `role`), exposing `isAuthenticated` (`!!token`) and `isAdmin` (`role === "admin"`).

Sessions persist in two places:

| Store | Details | Purpose |
|---|---|---|
| Cookie `auth` | `{ token, user }`, `maxAge` 7 days, `sameSite: "lax"`, `path: "/"` | **Readable on the server** — this is what makes SSR route guards correct |
| `localStorage` | Same payload | Client-side fallback |

The cookie is the load-bearing one. `app/plugins/auth.ts` hydrates the store from it and is **universal** — deliberately not named `auth.client.ts`. Plugins run before route middleware, so on a direct navigation to `/admin/dashboard` the server has already populated the store by the time guards evaluate. With a client-only plugin the server-side store would be empty and every guarded page would redirect a logged-in user on first load, then correct itself after hydration — a visible flash and a broken deep link.

The cookie holds the **role as well as the token**, precisely so guards can make role decisions during SSR without calling the API.

---

## Frontend route protection

Four middleware files. One is global; three are applied per page via `definePageMeta`.

| File | Scope | Rule |
|---|---|---|
| `role-area.global.ts` | **Global** | Authenticated users only: an admin outside `/admin/*` → `/admin/dashboard`; a customer inside `/admin/*` → `/`. Logged-out visitors pass through. |
| `admin.ts` | Per page | Logged out → `/admin/login`. Authenticated non-admin → `/`. |
| `auth.ts` | Per page | Logged out → `/auth/login`. |
| `guest.ts` | Per page | Authenticated visitors cannot see a login/register form: admin → `/admin/dashboard`, customer → `/`. |

### Why area separation has to be global

Three pages — `/`, `/products`, `/products/:id` — declare **no** middleware at all. A per-page rule can never cover a page that opts into no guard, so confining an admin to the admin area is only possible from a global middleware. This is why `role-area.global.ts` exists instead of the rule living inside `auth.ts`.

Global middleware runs on **every** navigation, before named per-page middleware, and on browser Back/Forward as well as clicks and direct loads.

### Behaviour matrix

Verified against the running server:

| Target | Logged out | Customer | Admin |
|---|---|---|---|
| `/`, `/products`, `/products/:id` | renders | renders | → `/admin/dashboard` |
| `/cart`, `/checkout`, `/orders`, `/orders/:id` | → `/auth/login` | renders | → `/admin/dashboard` |
| `/admin/dashboard` and other `/admin/*` | → `/admin/login` | → `/` | renders |
| `/auth/login`, `/auth/register` | renders | → `/` | → `/admin/dashboard` |
| `/admin/login` | renders | → `/` | → `/admin/dashboard` |

The two areas are **mutually exclusive** for a signed-in user. An accepted consequence: an admin cannot preview the storefront while signed in as admin — that requires logging out, or a customer account in a separate browser profile.

### Design constraints

Three rules keep the guards from looping. Preserve them when editing:

- **The global guard ignores logged-out visitors.** It returns early when there is no session, leaving `auth.ts` and `admin.ts` to own the logged-out redirects. Adding a logged-out branch would break the public storefront and both login pages.
- **`/admin/login` must never carry the `admin` middleware.** It carries `guest` only. Guarding it with `admin` would send a logged-out visitor to `/admin/login`, where the guard would fire again — an infinite redirect.
- **Redirect targets must not themselves be guarded against their audience.** `/admin/dashboard` accepts admins; `/` carries no middleware. Every redirect in the matrix above resolves in exactly one hop.

Redirects use `navigateTo(..., { replace: true })`. A plain push would stack history entries, so repeated Back presses would walk the user through a chain of redirects instead of leaving them where they are.

---

## Logout

`logout()` in the auth store clears `token` and `user`, deletes the cookie and the `localStorage` mirror, then navigates: an admin lands on `/admin/login`, a customer on `/auth/login`.

**The order is load-bearing.** State is cleared *before* navigating, because both destinations carry the `guest` guard. Navigating first would let the guard observe a live session and bounce the user straight back to their dashboard — logout would appear to do nothing at all. Do not reorder those statements.

The role is read into a local variable before the state is cleared, for the same reason: computing it afterwards would always yield `false` and every logout would land on the customer login page.

Logout is purely client-side. The JWT stays cryptographically valid until it expires — see below.

---

## Frontend guards are not security

The four middleware files are **user experience**. They keep people out of pages that would not work for them and stop the app offering irrelevant navigation.

They are not an authorization boundary. A user can edit the `auth` cookie or the Pinia store in devtools and reach any page in the app. What they *cannot* do is make the API answer: every protected endpoint re-validates the token's signature and role server-side and returns `401` or `403` regardless of what the client believes.

Consequences for anyone working on this code:

- **Never let an authorization decision exist only in the frontend.** If an action must be restricted, the restriction belongs on the endpoint.
- **A bypassed guard is a cosmetic bug, not a breach** — the admin pages simply render empty and log API errors.
- **Do not treat data returned to the client as hidden.** Anything an endpoint returns is visible, whatever the UI shows.

---

## Known gaps

Verified, currently unfixed. Each is a candidate follow-up rather than a bug to work around silently.

**Password policy mismatch between client and server.** `app/utils/validation.ts` accepts any password of 6+ characters. `OnlineStore.API/Dtos/RegisterRequest.cs` requires 8+ with at least one uppercase letter and one digit. A password like `abcdef` passes client validation and is then rejected by the API with a `400`, so the user sees a server error where they expected inline field feedback. The API is the stricter and correct one; the client schema should be tightened to match.

**Role comparison is case-sensitive on both sides.** `Roles = "admin"` and `role === "admin"` both require the exact lowercase string. A row promoted to `'Admin'` yields a user who is not an admin to either half, and who is *also* excluded from customer pages by the global area guard — effectively locked out. Nothing normalises the value on write or read.

**No token revocation.** Tokens are valid for 7 days and nothing can invalidate one early. Logging out only discards the client's copy; demoting a user in SQL does not end their session. The only blunt instrument is rotating `Jwt:Key`, which invalidates **every** token at once. A real revocation story needs shorter lifetimes plus refresh tokens, or a server-side deny list.

**Registration cannot create an admin, by design — but there is no admin management UI either.** Promotion and demotion are SQL-only operations. This is safe but operationally awkward, and there is no audit trail of who changed a role.

**`/auth/login` accepts any role.** An admin who logs in through the customer page authenticates successfully; the global area guard then immediately redirects them to `/admin/dashboard`. The outcome is correct, but the customer login page does not reject admin credentials the way `/admin/login` rejects customer ones.
