# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/admin-categories-crud/admin-categories-list-create-and-edit/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Admin Categories List, Create and Edit
- **Feature slug (folder under `plans/`):** `admin-categories-crud`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Admin Categories List, Create and Edit
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Add admin pages to manage Categories (and, since the backend story bundles them together, Brands too): a list page and create/edit forms. This is entirely new frontend surface — no placeholder currently exists — and hard-depends on the backend "Categories and Brands CRUD" story having shipped `GET/POST/PUT/DELETE /api/categories` and `/api/brands`.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] New page `app/pages/admin/categories/index.vue` (`layout: 'admin'`, `middleware: 'admin'`): fetches `GET /api/categories` (small list, no pagination needed per backend design) with the admin bearer token attached; table of name + edit/delete actions; "Add category" button linking to `/admin/categories/new`.
- [ ] New page `app/pages/admin/categories/new.vue`: single-field form (name), zod-validated (non-empty, reasonable max length), submits `POST /api/categories` as JSON (not multipart — categories have no image), toast + redirect on success, surfaces the backend's duplicate-name 400 message on failure.
- [ ] New page `app/pages/admin/categories/[id]/edit.vue`: same form pre-filled from the list (or a `GET /api/categories/{id}` if the backend exposes one — otherwise pass the name via route state/refetch the list), submits `PUT /api/categories/{id}`.
- [ ] Delete action on the list: calls `DELETE /api/categories/{id}` with a confirm dialog; since the backend blocks deletion when a Product still references the category (should return a clean 4xx per the backend story, not a 500), surface that message clearly ("cannot delete a category in use by products") rather than a generic error.
- [ ] Add a "Categories" nav item to `app/layouts/admin.vue`'s sidebar (currently only has Dashboard/Products).
- [ ] Decide (during planning) whether Brands get their own mirrored `/admin/brands` pages in this same story or a quick follow-up — the backend exposes identical CRUD for both, so mirroring the Category pages for Brand should be low-effort; if included, add a "Brands" sidebar item too.
- [ ] Once this ships, wire the admin product create/edit forms (from "Admin Products List, Create and Edit", if not already done) to use real `<select>` dropdowns populated from `GET /api/categories`/`GET /api/brands` instead of raw numeric id inputs.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| *(e.g. `attachments/flow.png`)* | *(e.g. UX flow)* |

*(Add rows per file. If none, write "None.")*

---

## Dependencies

- **Blocked by / related ids:** **Hard-blocked** by backend story "Categories and Brands CRUD" (`OnlineStore.API/.squad/stories/categories-and-brands/`) — do not start implementation until those endpoints exist and are verified working (e.g. via Swagger).
- **Depends on code areas or other stories:** `app/layouts/admin.vue` (sidebar), `app/middleware/admin.ts`, `app/utils/validation.ts`, and loosely "Admin Products List, Create and Edit" for the final follow-up wiring step.

## Extra notes (optional)

- Last story in the overall plan — implement after all customer-journey stories and the products admin story, once the backend categories/brands endpoints are confirmed live.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.
- Expected shape (confirm exact field names against the actually-implemented backend before coding): `GET /api/categories`/`GET /api/brands` → `{ id, name }[]`; writes are JSON `{ name }`, admin-only, `[AllowAnonymous]` on reads.
- Follow the exact same admin CRUD conventions established by the products admin story (auth header attachment, confirm-before-delete, zod + `fieldErrors()`, `BaseButton` loading state).

## Out of scope

- Any change to how Products select their category/brand beyond the follow-up wiring step noted in acceptance criteria.
- Category/brand images or descriptions (backend has none).
