# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/middelware/add-middelware/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `middelware`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `` _(used in filenames and plan tables; fill manually if empty)_
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

_(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)_

```
add-middelware
```

---

## Description

_(Paste the full work item description. Prefilled when fetched from a tracker.)_

```
Task: Implement Role-Based Route Protection (Admin vs Customer)

Description:
Set up authentication state management and route protection so that:
- Regular customers can browse the shop normally (no sidebar).
- Only admin users can access a separate admin area with its own layout
  (including a sidebar), completely distinct from the customer-facing shop.

---

Part 1 — Store the user's role after login

When a user logs in successfully, store both the token AND the role
(and any other needed user info) using Pinia, not just localStorage
directly. This makes the auth state reactive across the whole app.

stores/auth.ts:
- state: { token: string | null, user: { name, email, role } | null }
- actions: login(), logout(), loadFromStorage()
- getters: isAuthenticated, isAdmin

On successful login/register API call:
1. Save token + user info in the Pinia store.
2. Persist them in localStorage as well (so state survives page refresh).
3. On app initialization (e.g. in app.vue or a plugin), restore the state
   from localStorage into the Pinia store.

---

Part 2 — Create two separate layouts

layouts/default.vue
- Used for all customer-facing pages (home, products, cart, etc.)
- No sidebar — just a simple header/footer (or whatever the shop design
  needs).

layouts/admin.vue
- Used ONLY for admin pages.
- Includes a sidebar with navigation (Dashboard, Products, etc.)
- Completely separate structure from default.vue.

Pages assign their layout explicitly:
<script setup>
definePageMeta({ layout: 'admin' })
</script>

---

Part 3 — Create route protection middleware

middleware/auth.ts
- Checks if the user is authenticated (has a valid token in the store).
- If not, redirect to /auth/login.
- Apply this middleware to any page that requires being logged in
  (e.g. cart, checkout, orders).

middleware/admin.ts
- Checks if the user is authenticated AND their role is "admin".
- If not authenticated → redirect to /auth/login.
- If authenticated but NOT admin → redirect to home page "/"
  (or a "403 Forbidden" page).
- Apply this middleware to all pages under /admin/*.

Pages use middleware like this:
<script setup>
definePageMeta({ middleware: 'admin', layout: 'admin' })
</script>

---

Part 4 — Folder structure for pages

pages/
├── index.vue                    (layout: default)
├── products/
│   ├── index.vue                (layout: default)
│   └── [slug].vue               (layout: default)
├── cart.vue                     (layout: default, middleware: auth)
├── auth/
│   ├── login.vue                (layout: default)
│   └── register.vue             (layout: default)
└── admin/
    ├── dashboard.vue             (layout: admin, middleware: admin)
    └── products/
        ├── index.vue             (layout: admin, middleware: admin)
        └── new.vue               (layout: admin, middleware: admin)

```

---

## Acceptance criteria

_(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)_

```
- [ ] After login, the user's role is stored and accessible via the
      Pinia auth store.
- [ ] Auth state survives a page refresh (restored from localStorage).
- [ ] Customer-facing pages use the default layout (no sidebar).
- [ ] Admin pages use a separate layout with a sidebar.
- [ ] Visiting any /admin/* page while logged out redirects to /auth/login.
- [ ] Visiting any /admin/* page as a "customer" role redirects away
      (not to the admin dashboard).
- [ ] Visiting any /admin/* page as an "admin" role works normally.
- [ ] Logging out clears the token and role from both the store and
      localStorage.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder)  | What it is       |
| ------------------------------- | ---------------- |
| _(e.g. `attachments/flow.png`)_ | _(e.g. UX flow)_ |

_(Add rows per file. If none, write "None.")_

---

## Dependencies

- **Blocked by / related ids:** (tracker ids only; optional short note)
- **Depends on code areas or other stories:**

## Extra notes (optional)

- Anything not captured above (e.g. chat context) — keep short.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.

## Out of scope

- What this story explicitly does **not** cover:
