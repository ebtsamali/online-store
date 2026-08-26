# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/middelware/admin-login-page/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Admin login separation
- **Feature slug (folder under `plans/`):** `middelware`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** ``
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

```
Separate admin login page from customer login, update middleware/logout to match
```

---

## Description

```
Today there is a single shared login page, `app/pages/auth/login.vue` at
route `/auth/login`, used by BOTH customers and admins. It posts to the
same `POST /auth/login` API, stores whatever role comes back via
`auth.login()`, and always redirects to `/` on success. There is no
admin-specific entry point: an admin currently has to log in through the
customer page and then navigate to `/admin/dashboard` manually.

This story gives admins their own dedicated login page, distinct from the
customer one, and brings the surrounding middleware/logout logic in line
with that split so an admin never gets bounced through customer-facing
auth screens.

---

Part 1 — New admin login page

Create `app/pages/admin/login.vue` at route `/admin/login`:
- Same mechanics as the existing `app/pages/auth/login.vue` (zod
  `loginSchema` validation via `app/utils/validation.ts`, the
  `BaseForm`/`BaseInput`/`BaseButton` components, `vue-sonner` toasts,
  `definePageMeta({ layout: false })` — no sidebar, this is a public,
  pre-auth screen).
- Distinct admin-facing copy/branding (e.g. "Admin sign in" instead of
  "Log in") so it's visually obvious this is not the storefront login.
- No "No account? Register" link — admins are not self-registered.
- Calls the SAME `POST /auth/login` endpoint (no new backend work).
- On success:
  - If the returned `role !== "admin"`, treat it as a rejected login:
    do NOT call `auth.login(...)`, show an error toast (e.g.
    "This login is for admins only"), and leave the form on the page.
    (Prevents a customer credential pair from creating a session via the
    admin screen.)
  - If `role === "admin"`, call `auth.login(...)` as today and redirect
    to `/admin/dashboard` (not `/`).
- On failure (401 etc.), same generic-message toast behavior as the
  existing login page.

The existing `/auth/login` page and its behavior for customers are
UNCHANGED by this story (still redirects to `/` on success).

---

Part 2 — Middleware consistency

`app/middleware/admin.ts` currently redirects unauthenticated visitors to
`/auth/login` (the customer page). Change this to redirect to
`/admin/login` instead, so anyone hitting a guarded `/admin/*` route
without a session lands on the admin login, not the customer one.

Leave the "authenticated but not admin" branch redirecting to `/`
(unchanged) — a logged-in customer who wanders into `/admin/*` should
still land on the customer home, not the admin login.

`app/middleware/auth.ts` (used by customer-protected routes like `/cart`,
`/orders`) is UNCHANGED — it keeps redirecting to `/auth/login`.

---

Part 3 — Logout consistency

`app/stores/auth.ts` `logout()` currently hardcodes
`return navigateTo("/auth/login")` regardless of who logged out. Once
there are two login pages, an admin who logs out from the admin layout
would incorrectly land on the customer login page. Update `logout()` so
an admin's session ends on `/admin/login` and a customer's session ends
on `/auth/login` (e.g. branch on the role that was just cleared, or
accept an optional redirect target from the caller — pick whichever
fits the existing store/plugin structure from Story 01 best).

Both logout call sites already exist and don't need new UI:
- `app/layouts/admin.vue` (two buttons call `auth.logout()`)
- `app/layouts/default.vue` (customer header)

---

Part 4 — Route/layout notes

- `/admin/login` must stay PUBLIC (no `middleware: 'admin'` or
  `middleware: 'auth'` on it) — same reasoning as `/auth/login` staying
  unguarded today, otherwise it redirects to itself in a loop.
- `/admin/login` uses `layout: false`, same as the customer login page —
  NOT the `admin` layout (that layout assumes an authenticated user and
  renders a sidebar + logged-in user info, which don't apply pre-login).
```

---

## Acceptance criteria

```
- [ ] Visiting `/admin/login` while logged out shows a distinct
      admin-branded login form (not the customer `/auth/login` page),
      with no sidebar/admin-layout chrome.
- [ ] Submitting valid admin credentials on `/admin/login` logs the user
      in and redirects to `/admin/dashboard`.
- [ ] Submitting valid credentials for a NON-admin account on
      `/admin/login` does not create a session and shows an error
      toast — the customer is not silently logged into the app.
- [ ] Submitting invalid credentials on `/admin/login` shows the same
      kind of error toast as the customer login page.
- [ ] `/auth/login` (customer page) behavior is unchanged: still logs
      any valid account in and redirects to `/`.
- [ ] Visiting any guarded `/admin/*` page while logged out redirects to
      `/admin/login` (not `/auth/login`).
- [ ] Visiting a guarded `/admin/*` page as an authenticated non-admin
      still redirects to `/` (unchanged).
- [ ] Visiting a customer-guarded page (e.g. `/cart`) while logged out
      still redirects to `/auth/login` (unchanged).
- [ ] Logging out from the admin layout lands on `/admin/login`.
- [ ] Logging out from the customer layout lands on `/auth/login`.
- [ ] `/admin/login` has no `auth`/`admin` middleware applied to it
      (stays reachable while logged out; no redirect loop).
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder)  | What it is       |
| ------------------------------- | ---------------- |
| _(e.g. `attachments/flow.png`)_ | _(e.g. UX flow)_ |

None.

---

## Dependencies

- **Blocked by / related ids:** none (tracker type is `none`)
- **Depends on code areas or other stories:**
  - [`01-story-auth-store-persistence.md`](../../../plans/middelware/01-story-auth-store-persistence.md) — `useAuthStore`, `login()`/`logout()`, cookie persistence.
  - [`02-story-layouts.md`](../../../plans/middelware/02-story-layouts.md) — `layouts/admin.vue` / `layouts/default.vue`.
  - [`03-story-route-protection-middleware.md`](../../../plans/middelware/03-story-route-protection-middleware.md) — `middleware/admin.ts` / `middleware/auth.ts`, the SSR-guard behavior this story must preserve.
  - [`04-story-auth-ui-forms-validation.md`](../../../plans/middelware/04-story-auth-ui-forms-validation.md) — `BaseForm`/`BaseInput`/`BaseButton`, `app/utils/validation.ts` `loginSchema`/`fieldErrors`, the toast pattern the new admin page reuses.

## Extra notes (optional)

- Open decision for whoever runs `/squad-plan`: should `/auth/login`
  also start REJECTING admin-role logins (forcing admins through
  `/admin/login` exclusively), symmetric with `/admin/login` rejecting
  customer logins? This intake's default keeps `/auth/login` unchanged
  (still accepts any role) to minimize blast radius on existing
  customer-facing behavior — flag if the stricter symmetric version is
  wanted instead.
- If a later story converts `admin.ts` from a per-page middleware into a
  global one guarding the whole `/admin/*` prefix (an option already
  called out as out-of-scope in Story 03), make sure `/admin/login` is
  excluded from that guard, or it will redirect-loop against itself.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.
- Backend contract is unchanged and external — same
  `POST /api/auth/login` used today, returning
  `{ token, name, email, role }`. See
  `../../../../OnlineStore.API/.squad/plans/middleware/03-authorization-middleware.md`.
- Current files this story touches:
  - `app/pages/auth/login.vue` — existing customer login, read for the
    pattern to replicate (zod validation, toasts, `BaseForm`/`BaseInput`/
    `BaseButton`, `layout: false`).
  - `app/middleware/admin.ts` — currently redirects unauthenticated to
    `/auth/login`; change target to `/admin/login`.
  - `app/middleware/auth.ts` — unchanged, still targets `/auth/login`.
  - `app/stores/auth.ts` — `logout()` currently hardcodes
    `navigateTo("/auth/login")`; needs to be role-aware.
  - `app/layouts/admin.vue` / `app/layouts/default.vue` — existing
    `auth.logout()` call sites, no new UI needed there.
  - `app/utils/validation.ts` — reuse `loginSchema`/`fieldErrors` as-is.

## Out of scope

- What this story explicitly does **not** cover:
  - No backend/API changes — one `/auth/login` endpoint continues to
    serve both pages.
  - No admin self-registration page (admins are provisioned elsewhere,
    not signed up).
  - No password-reset flow for either login page.
  - No changes to `/auth/register` (customer registration).
  - No visual redesign of the existing customer login page.
