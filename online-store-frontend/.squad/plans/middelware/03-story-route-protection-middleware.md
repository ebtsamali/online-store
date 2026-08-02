# Story 03 — Route-protection middleware & page structure

Add the two route middlewares (`auth`, `admin`), and create the minimal protected pages that consume them so the guard behaviour is demonstrable end-to-end: a customer-only `/cart` and an admin-only `/admin/*` area.

---

## Prerequisites

- **Story 01 completed** ([`01-story-auth-store-persistence.md`](./01-story-auth-store-persistence.md)): the `auth` store and the universal `auth` plugin that hydrates it from the cookie **on the server** — this is what makes the guards correct on a direct/hard navigation.
- **Story 02 completed** ([`02-story-layouts.md`](./02-story-layouts.md)): the `admin` layout the admin pages assign.

---

## Story Goal

1. `app/middleware/auth.ts` — require authentication; redirect to `/auth/login` otherwise.
2. `app/middleware/admin.ts` — require authentication **and** `role === "admin"`; redirect logged-out users to `/auth/login` and non-admins to `/`.
3. Minimal pages proving the guards work: `/cart` (`middleware: auth`, `default` layout) and `/admin/dashboard` + `/admin/products` (`middleware: admin`, `admin` layout).

**Not in scope:** real cart/checkout/order logic and real product-admin CRUD screens — these pages are functional placeholders to exercise the guards. `products/[slug]` and the customer `products` list are separate catalog features.

---

## Context — Read These Files First

1. [`01-story-auth-store-persistence.md`](./01-story-auth-store-persistence.md) — the store getters `isAuthenticated` and `isAdmin` are exactly what the guards check.
2. `.nuxt/tsconfig.app.json` — source root `app/`; route middleware lives in **`app/middleware/`** (auto-discovered; a file named `auth.ts` is referenced as `middleware: 'auth'`). `defineNuxtRouteMiddleware`, `navigateTo`, and `useAuthStore` are **auto-imported**.
3. `app/layouts/admin.vue` (Story 02) — the layout the admin pages set via `definePageMeta({ layout: 'admin' })`.
4. Backend authorization contract: [`../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md`](../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md) — the API enforces `[Authorize]`/`[Authorize(Roles="admin")]` server-side. **These frontend guards are UX only**; they do not replace server enforcement (see Edge Cases).

---

## Implementation tasks

### 1 — Auth middleware

**Create file: `app/middleware/auth.ts`**

```ts
export default defineNuxtRouteMiddleware(() => {
  const auth = useAuthStore();
  if (!auth.isAuthenticated) {
    return navigateTo("/auth/login");
  }
});
```

### 2 — Admin middleware

**Create file: `app/middleware/admin.ts`**

```ts
export default defineNuxtRouteMiddleware(() => {
  const auth = useAuthStore();
  if (!auth.isAuthenticated) {
    return navigateTo("/auth/login");
  }
  if (!auth.isAdmin) {
    // Authenticated but not an admin → send to the customer home, NOT the dashboard.
    return navigateTo("/");
  }
});
```

### 3 — Customer protected page: cart

**Create file: `app/pages/cart.vue`**

```vue
<script setup lang="ts">
definePageMeta({ middleware: "auth" }); // default layout (no explicit layout)
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold">Your Cart</h1>
    <p class="text-gray-500">Cart contents coming soon.</p>
  </div>
</template>
```

### 4 — Admin pages

**Create file: `app/pages/admin/dashboard.vue`**

```vue
<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold">Dashboard</h1>
    <p class="text-gray-500">Admin overview coming soon.</p>
  </div>
</template>
```

**Create file: `app/pages/admin/products/index.vue`**

```vue
<script setup lang="ts">
definePageMeta({ layout: "admin", middleware: "admin" });
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold">Products</h1>
    <p class="text-gray-500">Product management coming soon.</p>
  </div>
</template>
```

> These placeholders exist so every acceptance criterion about `/admin/*` and `/cart` is testable now. Real content is delivered by later catalog/cart/admin stories, which simply reuse the same `definePageMeta` guard/layout pattern.

---

## Edge Cases & Failure Modes

- **Direct/hard navigation under SSR (the critical case):** on a first hit to `/admin/dashboard`, the middleware runs **on the server**. It resolves correctly only because Story 01's universal `auth` plugin hydrated the store from the **cookie** before middleware ran. If the plugin were `.client`-only (or persistence were localStorage-only), the server store would be empty and the guard would wrongly redirect a logged-in admin. Confirm the plugin is `app/plugins/auth.ts` (universal), not `auth.client.ts`.
- **Logged-out visits `/admin/*`:** `admin.ts` first check → redirect to `/auth/login` (acceptance criterion).
- **Customer (non-admin) visits `/admin/*`:** `isAuthenticated` true, `isAdmin` false → redirect to `/` — **not** to `/admin/dashboard`, avoiding a redirect loop (acceptance criterion).
- **Admin visits `/admin/*`:** both checks pass → page renders in the `admin` layout (acceptance criterion).
- **Redirect target must be public:** `/auth/login` and `/` must have **no** guard (they don't — login has none in Story 01; `/` has none). If a guard were ever added to `/`, the non-admin redirect could loop.
- **Guards are UX-only, not security:** a determined user can manipulate the client store/cookie. The real protection is the backend's `[Authorize(Roles="admin")]` on the API (see the linked API plan). Admin pages that call protected endpoints will still get **401/403** from the server even if the client guard is bypassed. Do not treat the frontend guard as sufficient authorization.
- **Stale role after DB promotion/demotion:** the guard reads the role captured in the cookie at login. A role change requires re-login to take effect (documented in Story 01).
- **Applying `admin` middleware to the whole `/admin` subtree:** each admin page sets `middleware: 'admin'` explicitly (per the intake). If you later prefer a single rule for every `/admin/*` page, convert `admin.ts` to a global middleware guarded by `to.path.startsWith('/admin')` — **out of scope here**; the per-page approach matches the intake.

---

## Test Plan

No test runner is configured (see Story 01). Manual verification only. If integration testing is added later:

1. **Guard unit — `auth`:** unauthenticated store → returns a redirect to `/auth/login`; authenticated → returns `undefined` (allows).
2. **Guard unit — `admin`:** logged-out → `/auth/login`; customer role → `/`; admin role → allowed.
3. **E2E:** the six acceptance scenarios below (logged-out, customer, admin against `/admin/*`, plus `/cart` while logged out).

---

## Verification Steps

1. **Frontend runs:** `pnpm dev` (frontend) and **Backend runs:** `dotnet run --launch-profile http` (API), with `apiBase` aligned per Story 01.
2. **Logged-out `/cart`:** while logged out, visit `/cart` → redirected to `/auth/login`.
3. **Logged-out `/admin/dashboard`:** visit directly (hard refresh) → redirected to `/auth/login` (proves SSR guard works).
4. **Customer `/admin/*`:** log in as a `customer`, visit `/admin/dashboard` → redirected to `/` (not the dashboard).
5. **Admin `/admin/*`:** promote the user to `admin` in the DB, **log in again**, visit `/admin/dashboard` and `/admin/products` → both render inside the **admin sidebar layout**.
6. **Regression:** `/` and `/auth/login` remain reachable while logged out; `/cart` is reachable once logged in.

---

## Done Criteria

- [ ] `app/middleware/auth.ts` redirects unauthenticated users to `/auth/login`.
- [ ] `app/middleware/admin.ts` redirects logged-out → `/auth/login` and non-admins → `/` (acceptance: logged-out & customer redirects).
- [ ] `/admin/dashboard` and `/admin/products` render for an admin in the `admin` layout (acceptance: admin works normally).
- [ ] `/cart` requires authentication.
- [ ] Guards resolve correctly on a **direct SSR navigation**, not just client-side routing.
- [ ] Pages assign guard/layout via `definePageMeta`, matching the intake's folder structure.

---

**Feature complete. Report results to the user with the acceptance-criteria checklist filled in.**
