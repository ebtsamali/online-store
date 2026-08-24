# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/categories-and-brands/categories-and-brands-crud/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Categories and Brands CRUD
- **Feature slug (folder under `plans/`):** `categories-and-brands`

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
Categories and Brands CRUD
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
The frontend needs to let admins browse/manage Categories and Brands (list, create, edit) and let customers filter the product catalog by category/brand, but the API currently has no endpoints for either entity. `Product` already has `CategoryId`/`BrandId` FKs (Restrict on delete) but there is no way to list, create, update, or delete `Category`/`Brand` rows, and `GET /api/products` cannot filter by them.

Add full CRUD for Category and Brand, plus optional filtering on the products list endpoint, following the exact same conventions already used by `ProductsController` (DTU projections, admin-only writes, anonymous reads, ProblemDetails-style 400s).
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] `GET /api/categories` — public/anonymous, returns all categories `{ id, name }[]` ordered by name.
- [ ] `POST /api/categories` — admin only, body `{ name }`, 201 with created DTO, 400 on empty/duplicate name (case-insensitive unique).
- [ ] `PUT /api/categories/{id}` — admin only, body `{ name }`, 200 on success, 404 if missing, 400 on empty/duplicate name.
- [ ] `DELETE /api/categories/{id}` — admin only, 204 on success, 404 if missing, 409/400 (not 500) if any Product still references it (respect existing Restrict FK — return a friendly error instead of an unhandled DB exception).
- [ ] Same four endpoints mirrored for `GET/POST/PUT/DELETE /api/brands` with identical shape/rules.
- [ ] `GET /api/products` gains optional `categoryId` and `brandId` query params that filter the existing paged query (combinable with `search`); no change to existing behavior when the params are omitted.
- [ ] All new endpoints follow existing patterns: DTO projections only (never expose EF entities), `[Authorize(Roles="admin")]` on writes, `[AllowAnonymous]` on reads, validation errors returned as 400 with a clear message (same shape as ProductsController's existing 400s).
- [ ] Existing Products endpoints, DTOs, and behavior are unchanged except for the new optional filter params.
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

- **Blocked by / related ids:** none
- **Depends on code areas or other stories:** `ProductsController.cs`, `Product`/`Category`/`Brand` EF entities and DbContext — reuse existing patterns exactly. This unblocks the frontend admin Categories pages and product catalog filtering (separate frontend stories).

## Extra notes (optional)

- This gap was already flagged as "planned, not implemented" in the existing `.squad/stories/products/product-search-and-filtering/intake.md` — this story fills that gap for categories/brands specifically. Keep consistent with whatever that story already specifies for the `categoryId`/`brandId` filter params on `GET /api/products`, if it specifies exact param names/behavior.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `c#`.
- Mirror `ProductsController.cs` conventions: controller-level `[Authorize(Roles="admin")]` with `[AllowAnonymous]` on GETs, DTO records (not entities) as return types, EF Core queries via the existing DbContext.
- Category/Brand entities likely already exist as EF models referenced by `Product.CategoryId`/`Product.BrandId` — check `Models`/`Entities` folder before adding new ones.

## Out of scope

- No pagination on `GET /api/categories`/`GET /api/brands` (small reference lists, return all).
- No image/icon upload for categories or brands.
- No changes to Product creation/edit forms beyond accepting existing `categoryId`/`brandId` (already supported).
- No soft-delete; deletion is hard-delete blocked by existing FK Restrict, surfaced as a clean error.
