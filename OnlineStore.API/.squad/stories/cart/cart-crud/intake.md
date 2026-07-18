# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/cart/cart-crud/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):** Shopping Cart
- **Feature slug (folder under `plans/`):** `cart`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `cart-crud` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```

```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Task: Build Cart API (Add, View, Remove Items)

Description:
Implement the shopping cart functionality that allows a logged-in customer 
to add products to their cart, view the current cart contents, and remove 
items from it. The cart is tied to the authenticated user — each user only 
sees and manages their own cart.

Entity: CartItem
- Id (int, PK)
- UserId (int, FK, required)
- ProductId (int, FK, required)
- Quantity (int, required, default = 1)
- CreatedAt (DateTime, default = current UTC time)

Relationships:
- CartItem belongs to one User (UserId)
- CartItem belongs to one Product (ProductId)
- A user can have multiple CartItems (one per distinct product)

DTOs:
- AddToCartRequest(int ProductId, int Quantity)
- CartItemDto(int Id, int ProductId, string ProductName, decimal Price, 
  int Quantity, decimal Subtotal)
- CartResponse(List<CartItemDto> Items, decimal Total)

---

Endpoints:

1. POST /api/cart
   - Access: [Authorize] (any logged-in user)
   - Description: Adds a product to the current user's cart. If the product 
     already exists in the cart, increase its quantity instead of creating 
     a duplicate row.
   - Request body: AddToCartRequest
   - Success response (200 OK or 201 Created): updated CartItemDto
   - Error response: 400 Bad Request if Quantity <= 0
   - Error response: 404 Not Found if the product doesn't exist or is 
     inactive (IsActive = false)
   - Error response: 400 Bad Request if requested Quantity exceeds 
     available Stock

2. GET /api/cart
   - Access: [Authorize] (any logged-in user)
   - Description: Returns all cart items belonging to the currently 
     authenticated user, along with the calculated total price.
   - Success response (200 OK): CartResponse
   - Note: The user is identified from the JWT token (via 
     ClaimTypes.NameIdentifier) — never trust a UserId sent from the client.

3. DELETE /api/cart/{id}
   - Access: [Authorize] (any logged-in user)
   - Description: Removes a single item from the current user's cart.
   - Success response: 204 No Content
   - Error response: 404 Not Found if the cart item doesn't exist
   - Error response: 403 Forbidden if the cart item belongs to a different 
     user (a user must never be able to delete another user's cart item)

---

Business logic notes:
- Always resolve the current user from the JWT token, not from the request 
  body — this prevents one user from modifying another user's cart.
- When adding a product already in the cart, update the existing 
  CartItem's Quantity instead of inserting a new row.
- Validate that Quantity never exceeds the product's available Stock.
- Subtotal per item = Price * Quantity; Total = sum of all Subtotals.

Error handling:
- 400 Bad Request → invalid quantity or insufficient stock
- 401 Unauthorized → returned automatically by [Authorize] if no/invalid token
- 403 Forbidden → attempting to delete another user's cart item
- 404 Not Found → product not found / inactive, or cart item not found
- 500 Internal Server Error → "Something went wrong"
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] A logged-in user can add a product to their cart.
- [ ] Adding the same product twice increases its quantity instead of 
      creating a duplicate entry.
- [ ] A user can only view their own cart items, never another user's.
- [ ] A user can remove an item from their own cart.
- [ ] A user cannot remove another user's cart item (403 Forbidden).
- [ ] Adding a product with quantity exceeding available stock is rejected.
- [ ] Unauthenticated requests to any cart endpoint return 401 Unauthorized.
```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| None. | |

*(Add rows per file. If none, write "None.")*

---

## Dependencies

- **Blocked by / related ids:** (tracker ids only; optional short note)
- **Depends on code areas or other stories:**

## Extra notes (optional)

- Anything not captured above (e.g. chat context) — keep short.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `c#`.

## Out of scope

- What this story explicitly does **not** cover:
