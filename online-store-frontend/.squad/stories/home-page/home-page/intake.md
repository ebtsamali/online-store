# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/home-page/home-page/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `home-page`

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
home-page
```

---

## Description

_(Paste the full work item description. Prefilled when fetched from a tracker.)_

```
# Task: Home Page (Customer) & Admin Dashboard Home

## Context

This story builds on the existing groundwork from `add-middelware`: Pinia auth store (with `role`), `default` layout (customer) and `admin` layout (with sidebar), and the `auth`/`admin` route middleware. This story implements the actual **home views** for each layout — it does not add new middleware or auth logic.

---

## Part 1 — Customer Home Page (`pages/index.vue`, layout: default)

Public page, no auth required. Sections to build:

1. **Hero section** — banner/intro area for the store (brand message, primary CTA button e.g. "Shop now" linking to `/products`).
2. **Product highlights** — a grid of products pulled from `GET /api/products` (public, `[AllowAnonymous]`). Since there's no "featured" or "best-seller" flag in the backend yet, use the first page of results (`?page=1&pageSize=8`, newest-first as the API already orders by `CreatedAt` descending) as "New arrivals" rather than claiming they're "featured".
3. **Categories/Brands teaser** — note: there is currently no `GET /api/categories` or `GET /api/brands` endpoint (out of scope in the backend plan). Either skip this section for now, hardcode a static teaser, or flag it as a follow-up backend story if you want it data-driven.
4. **Call-to-action / footer band** — simple closing section (e.g. newsletter signup placeholder, or "Browse full catalog" link) — static, no backend needed.

No cart or account state is required to render this page; if the user is logged in, the header (from `default` layout) can still show their name via the auth store.

---

## Part 2 — Admin Dashboard Home (`pages/admin/dashboard.vue`, layout: admin, middleware: admin)

This is a separate page, not a reuse of the customer home — different layout, different purpose.

1. **Header** — shows the logged-in admin's name (and role badge), pulled from the Pinia auth store (already populated via `GET /api/auth/me` / stored on login). Include a logout action.
2. **Sidebar** — persistent nav (Dashboard, Products, [future: Orders, Users]) — already scaffolded in the admin layout per Story 02; this page just needs to render inside it.
3. **Dashboard summary cards** — quick stats using data already available from existing endpoints:
   - Total products → count from `GET /api/products` response (`total` field is already returned in the paged response).
   - Note: there is no dedicated `/api/admin/stats` or order-count endpoint yet. Don't fabricate numbers for orders/revenue — either omit those cards for now or flag as a backend follow-up story once Orders/Products stats endpoints exist.
4. **Recent products table** (optional nice-to-have) — small table of latest products via the same `GET /api/products?page=1&pageSize=5`, with edit/delete links into the existing admin product CRUD pages.

---

## Design Requirements

- Professional, clean e-commerce look — generous whitespace, clear visual hierarchy, consistent spacing scale (Tailwind spacing tokens, not arbitrary px values).
- **Primary color:** `#1C3684` — apply as the Tailwind theme's primary color (e.g. define in `tailwind.config` as `primary: '#1C3684'` with a few tint/shade steps for hover/active states), used consistently across CTA buttons, active nav item in the admin sidebar, links, and header accents.
- Admin dashboard should feel distinctly "back-office" (denser, data-forward) vs. the customer home (lighter, marketing-forward) — reinforcing that these are two different experiences, not the same template reused.

---

## Out of Scope

- Category/brand filtering or listing (no backend support yet).
- Real dashboard analytics/order stats (no backend support yet).
- Search functionality on the home page (product search exists on `/api/products?search=`, but wiring a search bar can be a separate story if needed).
```

---

## Acceptance criteria

_(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)_

```

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
