# Story 14 — Admin login page separation & middleware/logout consistency

Give admins a dedicated `/admin/login` page, distinct from the customer `/auth/login`, and make the route-protection middleware and the store's `logout()` consistent with that split so an admin is never bounced through customer-facing auth screens.

---

## Prerequisites

- **Story 01 completed** ([`01-story-auth-store-persistence.md`](./01-story-auth-store-persistence.md)): the `auth` store (`login()`/`logout()`, cookie persistence) this story extends.
- **Story 02 completed** ([`02-story-layouts.md`](./02-story-layouts.md)): `layouts/admin.vue` / `layouts/default.vue` — unchanged by this story, but their `auth.logout()` call sites are what pick up the new role-aware redirect.
- **Story 03 completed** ([`03-story-route-protection-middleware.md`](./03-story-route-protection-middleware.md)): `middleware/admin.ts` / `middleware/auth.ts`, whose unauthenticated-redirect target this story partially changes.
- **Story 04 completed** ([`04-story-auth-ui-forms-validation.md`](./04-story-auth-ui-forms-validation.md)): `BaseForm`/`BaseInput`/`BaseButton`, `app/utils/validation.ts` (`loginSchema`, `fieldErrors`), `vue-sonner` toasts — all reused as-is by the new admin page.

---

## Story Goal

1. `app/pages/admin/login.vue` — a new, public, admin-branded login page at `/admin/login`, built the same way as the existing customer login but rejecting non-admin credentials and landing on `/admin/dashboard`.
2. `app/middleware/admin.ts` — unauthenticated visitors redirect to `/admin/login` instead of `/auth/login`. The "authenticated but not admin" branch keeps redirecting to `/`.
3. `app/stores/auth.ts` `logout()` — becomes role-aware: an admin session ends on `/admin/login`, a customer session ends on `/auth/login`.

**Not in scope:** any backend/API change (same `POST /auth/login` endpoint serves both pages), an admin self-registration page, password reset, or any change to `/auth/register`. `/auth/login`'s own behavior for customers is intentionally left unchanged — it still accepts and logs in any role and redirects to `/` (see Edge Cases).

---

## Context — Read These Files First

1. [`app/pages/auth/login.vue`](../../../app/pages/auth/login.vue) — the pattern the new page replicates: zod validation via `loginSchema`/`fieldErrors`, `BaseForm`/`BaseInput`/`BaseButton`, `vue-sonner` toasts, `definePageMeta({ layout: false })`.
2. [`app/middleware/admin.ts`](../../../app/middleware/admin.ts) / [`app/middleware/auth.ts`](../../../app/middleware/auth.ts) — current redirect targets; only `admin.ts`'s unauthenticated branch changes.
3. [`app/stores/auth.ts`](../../../app/stores/auth.ts) — `logout()` currently hardcodes `navigateTo("/auth/login")`; `isAdmin` getter reads `state.user?.role === "admin"`.
4. [`app/layouts/admin.vue`](../../../app/layouts/admin.vue) / [`app/layouts/default.vue`](../../../app/layouts/default.vue) — existing `auth.logout()` call sites; no changes needed here, they just inherit the new redirect behavior.
5. `app/utils/validation.ts` (Story 04) — `loginSchema` and `fieldErrors` are reused unmodified.

---

## Implementation tasks

### 1 — Role-aware logout

**Edit file: `app/stores/auth.ts`**

Capture the role **before** clearing state, then branch the redirect:

```ts
logout() {
  const wasAdmin = this.isAdmin;
  this.token = null;
  this.user = null;
  authCookie().value = null;
  if (import.meta.client) {
    localStorage.removeItem("auth");
  }
  return navigateTo(wasAdmin ? "/admin/login" : "/auth/login");
},
```

No call-site changes needed — both `layouts/admin.vue` and `layouts/default.vue` already call `auth.logout()` with no arguments.

### 2 — Admin middleware redirect target

**Edit file: `app/middleware/admin.ts`**

```ts
export default defineNuxtRouteMiddleware(() => {
  const auth = useAuthStore();
  if (!auth.isAuthenticated) {
    return navigateTo("/admin/login");
  }
  if (!auth.isAdmin) {
    // Authenticated but not an admin → send to the customer home, NOT the dashboard.
    return navigateTo("/");
  }
});
```

`app/middleware/auth.ts` is **not modified** — customer-protected routes (`/cart`, `/orders`) keep redirecting to `/auth/login`.

### 3 — Admin login page

**Create file: `app/pages/admin/login.vue`**

```vue
<script setup lang="ts">
import { toast } from "vue-sonner";
import { loginSchema, fieldErrors } from "~/utils/validation";

definePageMeta({ layout: false });

const auth = useAuthStore();
const apiBase = useApi();

const form = reactive({ email: "", password: "" });
const errors = ref<Record<string, string>>({});
const loading = ref(false);

async function onSubmit() {
  errors.value = {};

  const parsed = loginSchema.safeParse(form);
  if (!parsed.success) {
    errors.value = fieldErrors(parsed.error);
    return;
  }

  loading.value = true;
  try {
    const res = await $fetch<{
      token: string;
      name: string;
      email: string;
      role: string;
    }>(`${apiBase}/auth/login`, { method: "POST", body: parsed.data });

    if (res.role !== "admin") {
      toast.error("This sign-in is for admins only.");
      return;
    }

    auth.login(res.token, { name: res.name, email: res.email, role: res.role });
    toast.success(`Welcome back, ${res.name}`);
    await navigateTo("/admin/dashboard");
  } catch (e: any) {
    toast.error(e?.data?.message ?? "Invalid email or password");
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="min-h-screen bg-[#f5f6fa] flex items-center justify-center p-4">
    <div class="w-full max-w-md rounded-2xl bg-white p-8 shadow-lg sm:p-10">
      <div class="flex justify-center">
        <NuxtLink to="/">
          <img
            src="/images/logo.jpg"
            alt="Online Store"
            class="h-[128px] w-auto object-contain"
          />
        </NuxtLink>
      </div>

      <h1 class="mt-2 text-center text-3xl font-bold text-[#1b3a6b]">Admin sign in</h1>
      <p class="mt-1 text-center text-sm text-[#8a94a6]">Staff access only</p>

      <BaseForm class="mt-8" @submit="onSubmit">
        <BaseInput
          v-model="form.email"
          label="Email"
          type="email"
          autocomplete="email"
          placeholder="you@example.com"
          :error="errors.email"
        />

        <BaseInput
          v-model="form.password"
          label="Password"
          type="password"
          autocomplete="current-password"
          placeholder="••••••••"
          :error="errors.password"
        />

        <BaseButton type="submit" block :loading="loading">
          {{ loading ? "Signing in…" : "Sign in" }}
        </BaseButton>
      </BaseForm>

      <p class="mt-6 text-center text-sm text-[#8a94a6]">
        <NuxtLink to="/" class="font-semibold text-[#1b3a6b]">Back to store</NuxtLink>
      </p>
    </div>
  </div>
</template>
```

> No `middleware` key in `definePageMeta` — this page must stay public, same reasoning as `/auth/login` (see Edge Cases).

---

## Edge Cases & Failure Modes

- **Order of operations in `logout()`:** `wasAdmin` must be read from `this.isAdmin` **before** `this.user` is nulled out — computing it after would always evaluate `false` and every logout would land on `/auth/login`, silently breaking the admin case.
- **Non-admin credentials on `/admin/login`:** the API still authenticates them and returns a valid token (the backend has no separate admin-only endpoint), but the page must not call `auth.login(...)` and must not navigate — otherwise a customer account gets a real session via the admin screen. The token from that response is simply discarded client-side.
- **`/admin/login` must stay unguarded:** it takes no `middleware`. If `admin` middleware were ever applied here (e.g. by a future blanket `/admin/*` guard, called out as an option in Story 03), an unauthenticated visitor would redirect to `/admin/login` → guard runs again → redirect to itself, looping. Keep it explicitly excluded from any such future guard.
- **`layout: false`, not the `admin` layout:** `layouts/admin.vue` renders `auth.user?.name`/`role` and a sidebar that assume an already-authenticated session; using it pre-login would render broken/empty admin chrome around a login form.
- **`/auth/login` is intentionally unchanged:** an admin who logs in through the customer page still succeeds and lands on `/`, not `/admin/dashboard`. This is a deliberate scope boundary (see the intake's "Extra notes"), not an oversight — tightening this further (making `/auth/login` reject admin roles too) is a follow-up decision, not part of this story.
- **SSR direct navigation:** hitting `/admin/dashboard` cold while logged out still resolves server-side per Story 01's universal auth plugin; the only change is the redirect target (`/admin/login` instead of `/auth/login`), not the SSR mechanics.
- **Customer visiting `/admin/login` while already authenticated as a customer:** no guard exists to stop this (the page is public by design); submitting valid customer credentials again simply re-hits the "not admin" rejection branch. No special-case needed.

---

## Test Plan

No test runner is configured (see Story 01). Manual verification only. If integration testing is added later:

1. **Unit — `auth.logout()`:** starting from an admin session, `logout()` returns a redirect to `/admin/login`; starting from a customer session, it returns a redirect to `/auth/login`.
2. **Unit — `admin.ts`:** unauthenticated → `/admin/login`; authenticated non-admin → `/`; authenticated admin → allowed (unchanged from Story 03).
3. **Component — `admin/login.vue`:** invalid submit shows field errors, no `$fetch` call; mocked non-admin-role response → error toast, no store mutation, no navigation; mocked admin-role response → store updated, navigates to `/admin/dashboard`.

---

## Verification Steps

1. **Frontend runs:** `pnpm dev` (frontend) and **Backend runs:** `dotnet run --launch-profile http` (API).
2. **Admin login visual:** `/admin/login` renders the centred card with admin-specific copy ("Admin sign in" / "Staff access only"), no sidebar, no register link.
3. **Admin success:** log in with an admin account on `/admin/login` → success toast, redirected to `/admin/dashboard`, admin layout renders with the sidebar.
4. **Non-admin rejection:** log in with a customer account on `/admin/login` → error toast ("This sign-in is for admins only."), still on `/admin/login`, no session created (refresh the page — still logged out).
5. **Wrong credentials:** submit an invalid password on `/admin/login` → generic invalid-credentials toast, same as the customer page.
6. **Guarded `/admin/*` while logged out:** visit `/admin/dashboard` directly → redirected to `/admin/login` (not `/auth/login`).
7. **Guarded `/admin/*` as customer:** log in as a customer, visit `/admin/dashboard` → redirected to `/` (unchanged from Story 03).
8. **Customer guard unaffected:** logged out, visit `/cart` → still redirected to `/auth/login`.
9. **Logout targets:** log in as admin, click "Log out" in the admin layout → lands on `/admin/login`. Log in as a customer, click "Log out" in the default layout → lands on `/auth/login`.
10. **Regression:** `/auth/login` still logs any role in and redirects to `/`, unchanged.

---

## Done Criteria

- [ ] `app/pages/admin/login.vue` exists, is public, admin-branded, and uses `layout: false`.
- [ ] Valid admin credentials on `/admin/login` create a session and redirect to `/admin/dashboard`.
- [ ] Valid non-admin credentials on `/admin/login` create **no** session and show an error toast.
- [ ] Invalid credentials on `/admin/login` show the generic invalid-credentials toast.
- [ ] `/auth/login` behavior is unchanged (any role, redirects to `/`).
- [ ] `app/middleware/admin.ts` redirects unauthenticated visitors to `/admin/login`; non-admin authenticated visitors still redirect to `/`.
- [ ] `app/middleware/auth.ts` is unchanged (`/cart`, `/orders` still redirect to `/auth/login`).
- [ ] `auth.logout()` redirects an admin to `/admin/login` and a customer to `/auth/login`.
- [ ] `pnpm build` is clean.

---

**Feature complete. Report results to the user with the acceptance-criteria checklist filled in.**
