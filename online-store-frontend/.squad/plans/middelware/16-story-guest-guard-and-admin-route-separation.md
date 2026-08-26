# Story 16 — Strict admin/customer area separation & guest guard on auth pages

Give the app **two mutually exclusive areas**. An authenticated admin may only be inside `/admin/*`; an authenticated customer may only be outside it. Neither role can reach a login or register screen while signed in. These are the defects reported in [`.squad/stories/middelware/middelware-bug/intake.md`](../../stories/middelware/middelware-bug/intake.md).

---

## Revision note (supersedes the first pass of this story)

The first implementation of this story added the admin-eviction check **inside** [`app/middleware/auth.ts`](../../../app/middleware/auth.ts). That was insufficient and the bug survived: `middleware: "auth"` is declared on only four pages (`cart.vue`, `checkout.vue`, `orders/index.vue`, `orders/[id].vue`), so it never ran for the three storefront pages that declare **no middleware at all** — `app/pages/index.vue`, `app/pages/products/index.vue`, `app/pages/products/[id].vue`. An admin could still browse `/`, `/products` and `/products/{id}`.

Per-page guards cannot close this: a page with no `definePageMeta` middleware is reachable by definition. Area separation must therefore be enforced by a **global** middleware that runs on every route, which Story 03 flagged as an available option and deliberately deferred. This revision:

- **Adds** `app/middleware/role-area.global.ts` as the single source of truth for area separation.
- **Reverts** the `isAdmin` branch this story previously added to `app/middleware/auth.ts` — the global middleware now covers every route, so keeping a second copy of the rule in a per-page guard is duplicate logic that can drift.
- **Reverts** the admin-aware nav links this story previously added to `app/layouts/default.vue` — an admin can no longer render a default-layout page at all, so those `v-if="auth.isAdmin"` branches became dead code.
- **Keeps** `app/middleware/guest.ts` and the three `definePageMeta` edits unchanged; `guest` is still required (see Edge Cases).

---

## Prerequisites

- **Story 01 completed** ([`01-story-auth-store-persistence.md`](./01-story-auth-store-persistence.md)): the `auth` store and the **universal** plugin [`app/plugins/auth.ts`](../../../app/plugins/auth.ts) that hydrates it from the cookie **before** route middleware runs. Global middleware is correct on a direct/SSR navigation only because of this.
- **Story 02 completed** ([`02-story-layouts.md`](./02-story-layouts.md)): [`app/layouts/default.vue`](../../../app/layouts/default.vue) / [`app/layouts/admin.vue`](../../../app/layouts/admin.vue) — neither is modified by this revision.
- **Story 03 completed** ([`03-story-route-protection-middleware.md`](./03-story-route-protection-middleware.md)): `app/middleware/auth.ts` / `app/middleware/admin.ts`. Its Edge Cases section names converting to a global `/admin/*` guard as the alternative to per-page guards — **this story takes that option**.
- **Story 14 completed** ([`14-story-admin-login-page.md`](./14-story-admin-login-page.md)): `/admin/login` exists and is public; `logout()` is role-aware. Story 14's Edge Cases explicitly left "authenticated user visiting a login page" unguarded — this story closes that gap.

---

## Product rules (from story)

`ADMIN AREA` means `to.path` equal to `/admin` or starting with `/admin/`. Everything else is `CUSTOMER AREA`.

| Signed in as | Target | Current behaviour (the bug) | New behaviour |
|---|---|---|---|
| **admin** | `/`, `/products`, `/products/{id}` | Page renders | Redirected to `/admin/dashboard` |
| **admin** | `/cart`, `/checkout`, `/orders`, `/orders/{id}` | Page renders | Redirected to `/admin/dashboard` |
| **admin** | `/auth/login`, `/auth/register` | Form renders | Redirected to `/admin/dashboard` |
| **admin** | `/admin/login` (typed URL or browser **Back** off the dashboard) | Login form renders | Redirected to `/admin/dashboard` |
| **admin** | any other `/admin/*` page | Page renders | **Unchanged** — page renders |
| **customer** | any `/admin/*` page, `/admin/login` included | `/admin/login` renders; other `/admin/*` already redirect to `/` | Redirected to `/` |
| **customer** | `/auth/login`, `/auth/register` | Form renders | Redirected to `/` |
| **customer** | `/`, `/products`, `/cart`, `/orders`, … | Page renders | **Unchanged** — page renders |
| **logged out** | `/`, `/products`, `/products/{id}`, `/auth/login`, `/auth/register`, `/admin/login` | Renders | **Unchanged** — renders |
| **logged out** | `/cart`, `/checkout`, `/orders*` | Redirect to `/auth/login` | **Unchanged** |
| **logged out** | `/admin/*` (except `/admin/login`) | Redirect to `/admin/login` | **Unchanged** |

**Accepted consequence:** an admin can no longer preview the customer storefront while signed in as admin. This is the explicit requirement ("when login as admin, he can't access user pages and vice versa"). Staff who need to see the storefront must log out, or use a separate customer account in another browser profile.

---

## Story Goal

1. **Create `app/middleware/role-area.global.ts`** — a global guard that runs on every route and enforces the two-area rule for **authenticated** users only: an admin outside the admin area is sent to `/admin/dashboard`; a customer inside the admin area is sent to `/`. Logged-out visitors are passed straight through to the existing per-page guards.
2. **Create `app/middleware/guest.ts`** and apply it to the three auth pages so an authenticated visitor cannot see a login/register form.
3. **Leave `app/middleware/auth.ts` and `app/middleware/admin.ts` at their Story 03/14 behaviour** — they keep owning the *logged-out* redirects, which the global guard deliberately does not touch.
4. **Leave both layouts unchanged.**

**Not in scope:** any backend/API change (the API's `[Authorize]` enforcement is untouched and remains the real authority); an admin "view storefront" affordance; password reset; admin self-registration.

---

## Context — Read These Files First

1. [`app/middleware/admin.ts`](../../../app/middleware/admin.ts) — 10 lines. **Do not modify.** Read it for the shape the new middleware copies (`defineNuxtRouteMiddleware`, `useAuthStore()`, `return navigateTo(...)`) and to see that it already redirects a logged-out visitor to `/admin/login` and an authenticated non-admin to `/`.
2. [`app/middleware/auth.ts`](../../../app/middleware/auth.ts) — **must end this story back at its 5-line Story 03 form**: authenticated check only, redirecting to `/auth/login`. If the earlier pass of this story left an `if (auth.isAdmin)` branch in it, remove that branch.
3. [`app/stores/auth.ts`](../../../app/stores/auth.ts) — **lines 26–29** for the getters every guard reads (`isAuthenticated` is `!!state.token`; `isAdmin` is `state.user?.role === "admin"`), and **lines 41–50** for `logout()`. **Critical:** `logout()` nulls `this.token`/`this.user` (lines 43–44) **before** its `navigateTo` on line 49 — that ordering is what stops the new guards bouncing a logout (see Edge Cases).
4. Grep `definePageMeta` across `app/pages/` and note the three pages that declare **no** middleware — `app/pages/index.vue`, `app/pages/products/index.vue`, `app/pages/products/[id].vue`. These are exactly the pages the first pass of this story failed to cover, and the reason a global middleware is required.
5. [`app/pages/admin/login.vue`](../../../app/pages/admin/login.vue) — `definePageMeta` on **line 5** is the line to edit. Also read **lines 32–39**: the non-admin rejection branch and the `auth.login(...)` → `navigateTo("/admin/dashboard")` sequence. Note the "Back to store" `NuxtLink to="/"` near the end of the template — still correct, because only logged-out visitors ever see this page.
6. [`app/pages/auth/login.vue`](../../../app/pages/auth/login.vue) — `definePageMeta` on **line 5**; `auth.login(...)` → `navigateTo("/")` at **lines 32–34**.
7. [`app/pages/auth/register.vue`](../../../app/pages/auth/register.vue) — `definePageMeta` on **line 5**; `await navigateTo("/auth/login")` on **line 30** after a successful registration (still works — registration returns no token, so no session exists).
8. [`app/plugins/auth.ts`](../../../app/plugins/auth.ts) — 6 lines, **universal** (not `.client`). Plugins run before route middleware, which is why the global guard resolves correctly during SSR of a direct hit.
9. Nuxt global-middleware convention: a file in `app/middleware/` whose name ends in **`.global.ts`** runs on **every** route change without any `definePageMeta` opt-in, and runs **before** named per-page middleware. Multiple globals run in alphabetical filename order; this story adds the only one.
10. Precedent for tone and pattern: [`03-story-route-protection-middleware.md`](./03-story-route-protection-middleware.md) and [`14-story-admin-login-page.md`](./14-story-admin-login-page.md).

---

## Frontend Tasks

### 1 — Create the global area guard

**Create file: `app/middleware/role-area.global.ts`**

```ts
// Global: runs on EVERY route, so it also covers the storefront pages that
// declare no per-page middleware (/, /products, /products/[id]) — which is why
// this rule cannot live in `auth.ts`.
//
// Admin and customer areas are mutually exclusive for an AUTHENTICATED user.
// Logged-out visitors are passed through untouched: the per-page `auth`/`admin`
// guards own those redirects, and the public pages must stay public.
const ADMIN_ROOT = "/admin";
const ADMIN_LOGIN = "/admin/login";
const ADMIN_HOME = "/admin/dashboard";

export default defineNuxtRouteMiddleware((to) => {
  const auth = useAuthStore();
  if (!auth.isAuthenticated) {
    return;
  }

  // Exact boundary, not a bare startsWith("/admin") — that would also swallow a
  // sibling route such as "/administrators".
  const inAdminArea = to.path === ADMIN_ROOT || to.path.startsWith(ADMIN_ROOT + "/");

  if (auth.isAdmin) {
    // An admin belongs in the admin area, and never on the admin login form.
    if (!inAdminArea || to.path === ADMIN_LOGIN) {
      return navigateTo(ADMIN_HOME, { replace: true });
    }
    return;
  }

  // An authenticated customer never enters the admin area — /admin/login included.
  if (inAdminArea) {
    return navigateTo("/", { replace: true });
  }
});
```

`defineNuxtRouteMiddleware`, `navigateTo`, and `useAuthStore` are **auto-imported** — no import statements, matching `admin.ts` and `auth.ts`.

### 2 — Create the guest guard

**Create file: `app/middleware/guest.ts`**

```ts
// Public-only pages (login / register). An authenticated visitor is sent to the
// home appropriate to their role instead of being shown a sign-in form again.
// `replace: true` overwrites the auth-page history entry, so pressing Back after
// signing in cannot bounce the user between the form and their home.
export default defineNuxtRouteMiddleware(() => {
  const auth = useAuthStore();
  if (!auth.isAuthenticated) {
    return;
  }
  return navigateTo(auth.isAdmin ? "/admin/dashboard" : "/", { replace: true });
});
```

### 3 — Apply `guest` to the three auth pages

Each edit changes **one line** — the existing `definePageMeta({ layout: false })`. Keep `layout: false`; only add the `middleware` key.

**Edit file: `app/pages/admin/login.vue`** — line 5:

```ts
definePageMeta({ layout: false, middleware: "guest" });
```

**Edit file: `app/pages/auth/login.vue`** — line 5:

```ts
definePageMeta({ layout: false, middleware: "guest" });
```

**Edit file: `app/pages/auth/register.vue`** — line 5:

```ts
definePageMeta({ layout: false, middleware: "guest" });
```

No other change to these three files. In particular, **do not** remove the `res.role !== "admin"` rejection at `app/pages/admin/login.vue` lines 32–35 — guards run on navigation, not on form submit, so that branch is still the only thing stopping a customer creating a session through the admin screen.

### 4 — Restore `auth.ts` to its Story 03 form

**Edit file: `app/middleware/auth.ts`** — if the earlier pass of this story added an `if (auth.isAdmin)` branch, delete it. The file must read exactly:

```ts
export default defineNuxtRouteMiddleware(() => {
  const auth = useAuthStore();
  if (!auth.isAuthenticated) {
    return navigateTo("/auth/login");
  }
});
```

Area separation is now owned solely by `role-area.global.ts`. `app/middleware/admin.ts` is **not modified** by this story at all.

### 5 — Restore `layouts/default.vue` to its Story 02 form

**Edit file: `app/layouts/default.vue`** — if the earlier pass of this story added `v-if="auth.isAdmin"` / `!auth.isAdmin` branches to the `<nav>` block, revert them. An authenticated admin can no longer render a default-layout page, so those branches are unreachable. The `<nav>` must read:

```vue
<nav class="flex items-center gap-4 text-sm">
  <NuxtLink to="/products">Products</NuxtLink>
  <NuxtLink to="/cart">Cart</NuxtLink>
  <NuxtLink v-if="auth.isAuthenticated" to="/orders">Orders</NuxtLink>
  <NuxtLink v-if="!auth.isAuthenticated" to="/auth/login">Log in</NuxtLink>
  <button v-else class="text-red-600" @click="auth.logout()">Log out</button>
</nav>
```

`app/layouts/admin.vue` is **not** modified.

## Backend Tasks

No backend changes required. The API's `[Authorize]` / `[Authorize(Roles = "admin")]` enforcement is unchanged and remains the actual authorization boundary; everything in this story is client-side UX.

---

## Edge Cases & Failure Modes

- **The three unguarded storefront pages are the whole reason for a global guard.** `app/pages/index.vue`, `app/pages/products/index.vue` and `app/pages/products/[id].vue` declare no middleware. Any rule expressed as a per-page guard silently skips them — this is exactly how the first pass of this story shipped while the reported bug survived. Do not move this logic back into `auth.ts`.
- **`startsWith` boundary.** `to.path.startsWith("/admin")` alone would classify a hypothetical `/administrators` or `/admin-help` route as admin area. The guard tests `to.path === "/admin" || to.path.startsWith("/admin/")`. Enforced in `role-area.global.ts` (task 1).
- **`guest` is still required and is not redundant.** For an authenticated **customer** on `/auth/login`, the global guard passes (a customer outside the admin area is where they belong), so `guest` is the only thing that redirects them to `/`. The global guard covers the admin case for the same page; both agree on the outcome, and global middleware runs first.
- **Every redirect terminates in one hop — no loops.** Admin → `/admin/dashboard`: the guard re-runs for that path, `inAdminArea` is true and it is not `/admin/login`, so it allows. Customer → `/`: re-runs, customer outside the admin area, allows. Neither target is guarded by `guest`, and `/` carries no per-page middleware.
- **`logout()` ordering is load-bearing.** [`app/stores/auth.ts`](../../../app/stores/auth.ts) lines 41–50 clear `token`/`user` on lines 43–44 *before* navigating to `/admin/login` or `/auth/login` on line 49. Because the global guard returns early for an unauthenticated user, logout lands on a rendering login form. Reordering those statements (navigate first, clear after) would make both the global guard and `guest` see a live session and bounce the admin straight back to `/admin/dashboard` — logout would appear to do nothing. **Do not reorder.**
- **Logged-out visitors are deliberately untouched by the global guard.** It returns early on `!isAuthenticated`, leaving `auth.ts` → `/auth/login` and `admin.ts` → `/admin/login` as the only logged-out redirects. Adding a logged-out branch to the global guard would break the public storefront and both login pages.
- **`/admin/login` must keep having no `admin` middleware.** It carries `guest` only. Applying `admin` to it (or to a future blanket `/admin/*` rule) would make a logged-out visitor redirect to `/admin/login` → guard re-runs → redirect to itself, looping. The global guard is safe here precisely because it ignores logged-out users.
- **Browser Back after signing in (the originally reported symptom).** A popstate navigation still runs router guards, so both the global guard and `guest` fire. `{ replace: true }` matters: a plain `navigateTo` *pushes*, building a history stack the user must click through. `replace` overwrites the auth-page entry.
- **Guards run on navigation, not reactively.** Signing in on `/admin/login` mutates the store while that page is still mounted; the page is left only by its own `navigateTo("/admin/dashboard")` on line 39. If that call were removed the user would sit on a login form holding a live session — no guard would evict them. Keep the post-login `navigateTo` in all three pages.
- **Successful registration still reaches the login page.** `app/pages/auth/register.vue` line 30 navigates to `/auth/login` after `POST /auth/register`, which returns **no token** (comment on line 28), so no session exists and both guards pass. This breaks only if registration is ever changed to auto-login, in which case that target must become `/`.
- **"Back to store" on the login pages stays correct.** Both `app/pages/admin/login.vue` and `app/pages/auth/login.vue` link to `/`. Only logged-out visitors can see those pages, and `/` is public for them.
- **Role string is compared exactly.** `isAdmin` is `state.user?.role === "admin"` (`app/stores/auth.ts` line 28) — case-sensitive. If the API ever returns `"Admin"`, the global guard would treat a real admin as a customer and confine them to the storefront while `admin.ts` bounced them out of `/admin/*` — i.e. they could not reach either area's protected pages. This dependency predates this story; confirm the API's `role` casing during verification step 3.
- **Stale role in the cookie.** The guards read the role captured at login; a promotion or demotion in the DB requires re-login to change routing. Unchanged from Story 01.
- **Guards remain UX-only.** A user can edit the cookie or store to bypass all of this; protected API calls still return 401/403. Do not treat `role-area`/`guest`/`auth`/`admin` as authorization.

---

## Test Plan

No test runner is configured in this project (`package.json` has no `test` script — see Story 01), so verification is **manual**. If Vitest and `@nuxt/test-utils` are introduced later, add:

1. **Unit — `app/middleware/role-area.global.ts`**, driving `to.path` directly:
   - unauthenticated → returns `undefined` for every path, `/admin/dashboard` and `/` alike;
   - admin + `/admin/dashboard`, `/admin/products`, `/admin/products/3` → allows;
   - admin + `/`, `/products`, `/products/7`, `/cart`, `/checkout`, `/orders`, `/orders/2`, `/auth/login`, `/auth/register` → redirect to `/admin/dashboard`;
   - admin + `/admin/login` → redirect to `/admin/dashboard`;
   - customer + `/admin/login`, `/admin/dashboard`, `/admin/anything` → redirect to `/`;
   - customer + `/`, `/products`, `/cart` → allows;
   - **boundary:** admin + `/administrators` → treated as customer area (redirect), proving the path test is not a bare `startsWith`.
2. **Unit — `app/middleware/guest.ts`:** unauthenticated → allows; admin → `/admin/dashboard`; customer → `/`. Assert `{ replace: true }`.
3. **Unit — `app/middleware/auth.ts`:** unauthenticated → `/auth/login`; authenticated (either role) → allows. Regression that the `isAdmin` branch is gone.
4. **Unit — `app/middleware/admin.ts`:** unchanged from Story 03/14 — logged-out → `/admin/login`; customer → `/`; admin → allows.
5. **Unit — `auth.logout()`:** from an admin session → redirect to `/admin/login`; from a customer session → `/auth/login`. Regression cover for the ordering hazard.
6. **E2E:** the scenarios in Verification Steps 4–14 below.

---

## Verification Steps

1. **Frontend runs:** `pnpm dev` in `online-store-frontend/`. **Backend runs:** `dotnet run --launch-profile http` in `OnlineStore.API/`.
2. **Frontend builds:** `pnpm build` in `online-store-frontend/` — must be clean.
3. **Role casing check:** log in as the admin account and confirm the API's `/auth/login` response has `role` exactly `"admin"` (devtools → Network). Every guard depends on it.
4. **Admin cannot reach the storefront (the reported defect):** signed in as admin, visit `/`, `/products`, and a `/products/{id}` URL → each redirects to `/admin/dashboard`.
5. **Admin cannot reach customer-account pages:** signed in as admin, visit `/cart`, `/checkout`, `/orders`, and an `/orders/{id}` URL → each redirects to `/admin/dashboard`.
6. **Admin cannot reach any auth form:** signed in as admin, visit `/auth/login`, `/auth/register`, `/admin/login` → each redirects to `/admin/dashboard`.
7. **Back-button case:** log in on `/admin/login` → lands on `/admin/dashboard`. Press browser **Back** → stays on `/admin/dashboard`. Press Back again → still not the login form.
8. **Admin area still fully works:** signed in as admin, navigate `/admin/dashboard`, `/admin/products`, `/admin/products/new`, `/admin/categories`, `/admin/brands` and an edit page → all render in the admin layout, sidebar links work.
9. **Customer cannot reach the admin area:** signed in as a customer, visit `/admin/dashboard`, `/admin/products`, and `/admin/login` → each redirects to `/`.
10. **Customer cannot reach auth forms:** signed in as a customer, visit `/auth/login` and `/auth/register` → both redirect to `/`.
11. **Customer area still fully works:** signed in as a customer, browse `/`, `/products`, `/products/{id}`, `/cart`, `/checkout`, `/orders` → all render normally.
12. **Hard refresh (SSR path):** signed in as admin, hard-refresh directly on `/products` → redirected to `/admin/dashboard` with no flash of the storefront. Repeat signed in as a customer on `/admin/dashboard` → redirected to `/`.
13. **Logout works for both roles:** as admin, Log out from the admin layout → lands on `/admin/login` and the form **renders**. As a customer, Log out from the default layout → lands on `/auth/login` and the form renders.
14. **Registration flow intact:** logged out, register a new account → success toast, redirected to `/auth/login`, form renders. Log in as that customer → `/`.
15. **Regression — logged-out access:** logged out, `/`, `/products`, `/products/{id}`, `/auth/login`, `/auth/register` and `/admin/login` all render; `/cart` redirects to `/auth/login`; `/admin/dashboard` redirects to `/admin/login`.

---

## Done Criteria

- [ ] `app/middleware/role-area.global.ts` exists, is a `.global` middleware, and returns early for unauthenticated visitors.
- [ ] An authenticated **admin** is redirected to `/admin/dashboard` from every non-`/admin` path — including `/`, `/products`, `/products/[id]`, which no per-page guard covers.
- [ ] An authenticated **admin** is redirected to `/admin/dashboard` from `/admin/login`.
- [ ] An authenticated **admin** can still use every other `/admin/*` page normally.
- [ ] An authenticated **customer** is redirected to `/` from every `/admin/*` path, `/admin/login` included.
- [ ] An authenticated **customer** can still use `/`, `/products`, `/cart`, `/checkout`, `/orders*` normally.
- [ ] Neither role can render `/auth/login`, `/auth/register`, or `/admin/login` while signed in.
- [ ] The admin-area path test uses an exact boundary, not a bare `startsWith("/admin")`.
- [ ] `app/middleware/guest.ts` exists and the three auth pages declare `definePageMeta({ layout: false, middleware: "guest" })`.
- [ ] `app/middleware/auth.ts` is back to its 5-line Story 03 form (no `isAdmin` branch).
- [ ] `app/middleware/admin.ts` is unchanged.
- [ ] `app/layouts/default.vue` and `app/layouts/admin.vue` are unchanged from Story 02.
- [ ] `auth.logout()` lands on a **rendering** login page for both roles.
- [ ] Logged-out behaviour is unchanged: public pages render, `/cart` → `/auth/login`, `/admin/*` → `/admin/login`.
- [ ] `pnpm build` is clean.

---

**Feature complete. Report results to the user with the acceptance-criteria checklist filled in.**
