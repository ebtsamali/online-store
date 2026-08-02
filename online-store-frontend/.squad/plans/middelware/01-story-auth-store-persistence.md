# Story 01 — Auth store, persistence & login/register wiring

Create the reactive auth foundation the rest of this feature builds on: a Pinia `auth` store holding token + user, SSR-safe persistence, an app-init restore step, and minimal login/register pages that populate the store from the backend `/api/auth` endpoints.

---

## Prerequisites

- **None on the frontend** — this is the first story of the `middelware` feature and the first real app code beyond the scaffold.
- **Backend already provides the contract** (verified in the sibling API repo, `../../../../OnlineStore.API`):
  - `POST /api/auth/login` returns `AuthResponse(Token, Name, Email, Role)` → JSON `{ token, name, email, role }` (`Controllers/AuthController.cs` lines 60–81; `Dtos/AuthResponse.cs`).
  - `POST /api/auth/register` returns `RegisterResponse(Message, Name, Email)` → JSON `{ message, name, email }` with **no token and no role** (`AuthController.Register` lines 25–58; `Dtos/RegisterResponse.cs`). **Register does not log the user in.**
  - `User.Role` defaults to `"customer"` (`Entities/User.cs` line 9); the only way to get an `admin` is a manual DB update — there is no admin-registration endpoint.
- **Backend auth pipeline** is done: see the API-side plan [`../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md`](../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md) for the JWT/`/api/auth/me` contract this frontend consumes.

---

## Story Goal

1. A Pinia store `auth` exposing `state { token, user }`, getters `isAuthenticated` / `isAdmin`, and actions `login()` / `logout()` / `loadFromStorage()`.
2. Auth state **survives a page refresh** and is **readable during SSR** (so the route guards in Story 03 work on a hard/direct navigation, not only after client-side routing).
3. Minimal `/auth/login` and `/auth/register` pages that call the backend and populate the store.

**Not in scope:** the layouts (Story 02), the route-protection middleware and protected/admin pages (Story 03), styling beyond a functional form, password-reset, refresh tokens, and any admin-creation flow.

---

## Context — Read These Files First

1. `nuxt.config.ts` — 11 lines. Confirms modules `@nuxtjs/tailwindcss` and **`@pinia/nuxt`** are already registered (line 5) and `runtimeConfig.public.apiBase` is **`"http://localhost:5000/api"`** (line 8). **Note the port mismatch** with the running backend (`5016`) — see Edge Cases.
2. `app/composables/useApi.ts` — 5 lines. `useApi()` returns `config.public.apiBase` (a **string**, not a fetch wrapper). Build request URLs as `` `${useApi()}/auth/login` ``.
3. `app/app.vue` — currently `<div><NuxtPage /></div>`. **Do not restructure it in this story** (Story 02 wraps it in `<NuxtLayout>`); you only add a plugin, store, and pages.
4. `.nuxt/tsconfig.app.json` — confirms the source root is **`app/`** (the `~/*` alias maps to `../app/*`, lines ~53). Therefore every file below lives under `app/`.
5. Verify the Pinia store directory: `@pinia/nuxt` auto-imports stores from `<srcDir>/stores` → **`app/stores/`** (default in `node_modules/@pinia/nuxt/dist/*.mjs`). `defineStore`, `useCookie`, `navigateTo`, `defineNuxtPlugin` are all **auto-imported** — do not add manual imports for them.
6. Precedent for plan tone/structure: [`../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md`](../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md).

---

## Product rules (from story)

- **Persistence decision (read carefully — deviates slightly from the intake's wording, with reason):** the intake says "persist in localStorage." The app runs **with SSR on** (no `ssr: false` in `nuxt.config.ts`), and the Story 03 route guards run **on the server** for the initial/direct navigation. `localStorage` does **not** exist on the server, so a localStorage-only design makes the admin guard fail on a hard refresh of `/admin/*`. Therefore the source of truth is a **cookie** (`useCookie('auth')`, readable on both server and client), and we **also** mirror to `localStorage` on the client to honour the intake literally. `loadFromStorage()` prefers the cookie. This single decision is what makes the acceptance criteria "survives refresh" **and** "direct-visit `/admin` redirects" both hold.
- **Register ≠ login:** because `register` returns no token, the register page must **redirect to `/auth/login`** on success — it must **not** attempt to store a token/role.

---

## Implementation tasks

### 1 — Create the auth store

**Create file: `app/stores/auth.ts`**

```ts
export interface AuthUser {
  name: string;
  email: string;
  role: string;
}

interface AuthState {
  token: string | null;
  user: AuthUser | null;
}

// One cookie holds the whole auth payload so BOTH token and role are
// readable during SSR (required by the route guards in Story 03).
// useCookie serialises objects to JSON automatically.
function authCookie() {
  return useCookie<{ token: string; user: AuthUser } | null>("auth", {
    maxAge: 60 * 60 * 24 * 7, // 7 days
    sameSite: "lax",
    path: "/",
  });
}

export const useAuthStore = defineStore("auth", {
  state: (): AuthState => ({ token: null, user: null }),

  getters: {
    isAuthenticated: (state): boolean => !!state.token,
    isAdmin: (state): boolean => state.user?.role === "admin",
  },

  actions: {
    login(token: string, user: AuthUser) {
      this.token = token;
      this.user = user;
      authCookie().value = { token, user };
      if (import.meta.client) {
        localStorage.setItem("auth", JSON.stringify({ token, user }));
      }
    },

    logout() {
      this.token = null;
      this.user = null;
      authCookie().value = null;
      if (import.meta.client) {
        localStorage.removeItem("auth");
      }
      return navigateTo("/auth/login");
    },

    // Runs on server (cookie) and client (cookie, then localStorage fallback).
    loadFromStorage() {
      const cookie = authCookie();
      if (cookie.value) {
        this.token = cookie.value.token;
        this.user = cookie.value.user;
        return;
      }
      if (import.meta.client) {
        const raw = localStorage.getItem("auth");
        if (raw) {
          try {
            const parsed = JSON.parse(raw) as { token: string; user: AuthUser };
            this.token = parsed.token;
            this.user = parsed.user;
          } catch {
            localStorage.removeItem("auth");
          }
        }
      }
    },
  },
});
```

### 2 — Restore state on app init

**Create file: `app/plugins/auth.ts`**

A **universal** plugin (not `.client`) so it also runs on the server, where it reads the cookie and populates the store **before** route middleware executes.

```ts
export default defineNuxtPlugin(() => {
  const auth = useAuthStore();
  auth.loadFromStorage();
});
```

> Plugins run before route middleware in Nuxt, so by the time Story 03's guards read `isAuthenticated` / `isAdmin`, the store is hydrated from the cookie on the same request.

### 3 — Login page

**Create file: `app/pages/auth/login.vue`**

Minimal functional form. On submit, POST to `/auth/login`, then call `auth.login(token, { name, email, role })` and redirect home.

```vue
<script setup lang="ts">
const auth = useAuthStore();
const apiBase = useApi();

const email = ref("");
const password = ref("");
const error = ref("");
const loading = ref(false);

async function onSubmit() {
  error.value = "";
  loading.value = true;
  try {
    const res = await $fetch<{
      token: string;
      name: string;
      email: string;
      role: string;
    }>(`${apiBase}/auth/login`, {
      method: "POST",
      body: { email: email.value, password: password.value },
    });
    auth.login(res.token, { name: res.name, email: res.email, role: res.role });
    await navigateTo("/");
  } catch {
    // Backend returns a generic 401 "Invalid email or password".
    error.value = "Invalid email or password";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="max-w-sm mx-auto py-16 space-y-4">
    <h1 class="text-2xl font-bold">Log in</h1>
    <form class="space-y-3" @submit.prevent="onSubmit">
      <input v-model="email" type="email" placeholder="Email" required class="w-full border p-2 rounded" />
      <input v-model="password" type="password" placeholder="Password" required class="w-full border p-2 rounded" />
      <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
      <button :disabled="loading" class="w-full bg-blue-600 text-white p-2 rounded disabled:opacity-50">
        {{ loading ? "Signing in…" : "Log in" }}
      </button>
    </form>
    <p class="text-sm">No account? <NuxtLink to="/auth/register" class="text-blue-600">Register</NuxtLink></p>
  </div>
</template>
```

### 4 — Register page

**Create file: `app/pages/auth/register.vue`**

Because `register` returns **no token**, on success **redirect to `/auth/login`** — do not touch the store.

```vue
<script setup lang="ts">
const apiBase = useApi();

const name = ref("");
const email = ref("");
const password = ref("");
const error = ref("");
const loading = ref(false);

async function onSubmit() {
  error.value = "";
  loading.value = true;
  try {
    await $fetch(`${apiBase}/auth/register`, {
      method: "POST",
      body: { name: name.value, email: email.value, password: password.value },
    });
    // Register does NOT return a token — send the user to log in.
    await navigateTo("/auth/login");
  } catch (e: any) {
    // 409 Conflict → "Email already exists".
    error.value = e?.data?.message ?? "Registration failed";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="max-w-sm mx-auto py-16 space-y-4">
    <h1 class="text-2xl font-bold">Create account</h1>
    <form class="space-y-3" @submit.prevent="onSubmit">
      <input v-model="name" placeholder="Name" required class="w-full border p-2 rounded" />
      <input v-model="email" type="email" placeholder="Email" required class="w-full border p-2 rounded" />
      <input v-model="password" type="password" placeholder="Password" required class="w-full border p-2 rounded" />
      <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
      <button :disabled="loading" class="w-full bg-blue-600 text-white p-2 rounded disabled:opacity-50">
        {{ loading ? "Creating…" : "Register" }}
      </button>
    </form>
    <p class="text-sm">Have an account? <NuxtLink to="/auth/login" class="text-blue-600">Log in</NuxtLink></p>
  </div>
</template>
```

---

## Edge Cases & Failure Modes

- **`apiBase` port mismatch (will break login until fixed):** `nuxt.config.ts` line 8 sets `apiBase` to `http://localhost:5000/api`, but the backend `launchSettings.json` serves the `http` profile at **`http://localhost:5016`**. Login/register `$fetch` calls will fail (connection refused / CORS) unless these agree. **Fix in this story:** update `apiBase` to `http://localhost:5016/api` (or run the API on 5000). The backend CORS policy allows origin `http://localhost:3000` (`Program.cs` line 45), which is Nuxt's default dev port — do not change the frontend dev port.
- **SSR + `localStorage`:** `localStorage` is `undefined` on the server. Every access is guarded by `import.meta.client` (store actions 1). Never read `localStorage` at module top level.
- **Cookie is the SSR source of truth:** if a user clears cookies but keeps localStorage (or vice-versa), `loadFromStorage()` prefers the cookie, then falls back to localStorage on the client — a client-only refresh still restores state.
- **Corrupt localStorage JSON:** `loadFromStorage()` wraps `JSON.parse` in try/catch and removes the bad key rather than throwing during hydration.
- **Register of an existing email:** backend returns **409** `{ message: "Email already exists" }`; the register page surfaces `e.data.message`.
- **Wrong credentials:** backend returns a **generic 401** `{ message: "Invalid email or password" }` (same message for unknown email and wrong password — do not try to distinguish them in the UI).
- **No admin can be created via the UI:** every registered user is `role: "customer"`. To exercise `isAdmin`, promote a user in the DB (`UPDATE "Users" SET "Role"='admin' WHERE "Email"='…';`) and **log in again** to mint a fresh token/role — Story 03 verification depends on this.
- **Stale role in cookie:** the cookie stores the role captured at login time. A DB role change is not reflected until the user logs in again (acceptable for this story; a `/api/auth/me` re-hydration is a future enhancement).

---

## Test Plan

No test tooling exists in this project yet (`package.json` has only `nuxt` scripts, no test runner). Add none in this story; verify manually per Verification Steps. If a Vitest + `@nuxt/test-utils` setup is added later, cover:

1. **Unit — store getters:** `isAuthenticated` is `false` with `token: null`, `true` once set; `isAdmin` is `true` only when `user.role === "admin"`.
2. **Unit — `login()`/`logout()`:** `login()` sets state + cookie + (client) localStorage; `logout()` clears all three and navigates to `/auth/login`.
3. **Unit — `loadFromStorage()`:** prefers cookie over localStorage; tolerates corrupt localStorage JSON.
4. **Component — login page:** mocked `$fetch` success populates the store and navigates to `/`; 401 shows the error message.

---

## Verification Steps

1. **Frontend runs:** `pnpm dev` in `online-store-frontend/` → app on `http://localhost:3000`.
2. **Backend runs:** in `OnlineStore.API/`, `dotnet run --launch-profile http` → API on `http://localhost:5016`. Confirm `apiBase` matches (Edge Cases).
3. **Register:** visit `/auth/register`, submit a new user → redirected to `/auth/login`; re-submitting the same email shows "Email already exists".
4. **Login:** log in with that user → redirected to `/`. In DevTools → Application, confirm both an `auth` **cookie** and an `auth` **localStorage** entry exist containing `token` + `user`.
5. **Refresh survival:** hard-refresh any page → in the Vue/Pinia devtools the `auth` store still has `token` and `user` (restored by the `auth` plugin).
6. **Logout:** call `auth.logout()` (temporarily wire a button, or via devtools) → cookie and localStorage `auth` entries are gone, store is cleared, and you land on `/auth/login`.
7. **Admin role:** promote the user in the DB, log in again, and confirm `useAuthStore().isAdmin` is `true` (needed by Story 03).

---

## Done Criteria

- [ ] `app/stores/auth.ts` exposes `state { token, user }`, getters `isAuthenticated`/`isAdmin`, actions `login`/`logout`/`loadFromStorage` (acceptance: role stored & accessible via Pinia).
- [ ] Auth state survives a page refresh — restored on app init by `app/plugins/auth.ts` (acceptance: survives refresh).
- [ ] Token + user are readable during SSR (cookie) so Story 03 guards work on direct navigation.
- [ ] `/auth/login` populates the store from `POST /api/auth/login`; `/auth/register` redirects to login on success (no token handling).
- [ ] `logout()` clears the store, the cookie, and localStorage (acceptance: logout clears token & role from store and storage).
- [ ] `nuxt.config.ts` `apiBase` points at the running backend.

---

**STOP HERE. Report to the user and wait for confirmation before proceeding to Story 02.**
