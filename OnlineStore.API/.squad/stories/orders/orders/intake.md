# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/orders/orders/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `orders`

## Tracker (metadata only)

- **Tracker type:** `none`
- **Work item id:** `orders` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** ``
- **Status:** ``
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Order placement / checkout
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
Task: Implement Order Checkout, Payment Simulation & Order History

Description:
Implement the final step of the customer purchase flow: converting the 
current cart into a confirmed Order, simulating a payment process (no real 
payment gateway), decreasing product stock accordingly, and allowing the 
customer to view their past orders. Cancellation is out of scope for now.

---

Entity: Order
- Id (int, PK)
- UserId (int, FK, required)
- Status (string, default = "pending") — values: "pending", "paid", "failed"
- Total (decimal, required)
- CreatedAt (DateTime, default = current UTC time)

Entity: OrderItem
- Id (int, PK)
- OrderId (int, FK, required)
- ProductId (int, FK, required)
- ProductName (string, required) — snapshot of the product name at 
  purchase time
- UnitPrice (decimal, required) — snapshot of the price at purchase time 
  (never reference the live Product.Price afterwards)
- Quantity (int, required)

Note: ProductName and UnitPrice must be copied ("snapshotted") from the 
Product at the moment of checkout. If the product's price or name changes 
later, past orders must still show the price the customer actually paid.

---

DTOs:
- OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, 
  int Quantity, decimal Subtotal)
- OrderSummaryDto(int Id, string Status, decimal Total, DateTime CreatedAt)
- OrderDetailDto(int Id, string Status, decimal Total, DateTime CreatedAt, 
  List<OrderItemDto> Items)

---

Endpoints:

1. POST /api/orders/checkout
   - Access: [Authorize] (any logged-in user)
   - Description: Converts the current user's cart into a confirmed order.
   
   Business logic (must run as a single transaction — all steps succeed 
   together or all roll back):
   
   a. Load all CartItems belonging to the current user (identified from 
      the JWT token, never from the request body).
   b. If the cart is empty, return 400 Bad Request ("Cart is empty").
   c. For each cart item, verify the product still exists, is active, 
      and has enough Stock to fulfill the requested Quantity.
      - If any item fails this check, return 400 Bad Request identifying 
        which product is unavailable/insufficient, and do NOT create the 
        order.
   d. Create a new Order with Status = "pending" and Total = sum of all 
      (UnitPrice * Quantity).
   e. Create one OrderItem per cart item, snapshotting ProductName and 
      current Price at this moment.
   f. Decrease each Product's Stock by the purchased Quantity immediately.
   g. Simulate payment processing:
      - Introduce a short artificial delay (e.g. await Task.Delay(1500)) 
        to mimic a real payment gateway call.
      - Randomly or deterministically mark the order Status as "paid" 
        (for this simulation, treat it as always succeeding — no real 
        failure path needed unless you want to test the failure UI).
   h. Clear all CartItems belonging to the user (empty the cart).
   i. Return the created OrderDetailDto.
   
   Success response (201 Created): OrderDetailDto
   Error response: 400 Bad Request if cart is empty or stock is insufficient
   Error response: 401 Unauthorized if not logged in

2. GET /api/orders
   - Access: [Authorize] (any logged-in user)
   - Description: Returns a list of all past orders belonging to the 
     current user, most recent first.
   - Success response (200 OK): List<OrderSummaryDto>
   - Note: Only returns orders belonging to the authenticated user — 
     never another user's orders.

3. GET /api/orders/{id}
   - Access: [Authorize] (any logged-in user)
   - Description: Returns full details of a single order, including its 
     items.
   - Success response (200 OK): OrderDetailDto
   - Error response: 404 Not Found if the order doesn't exist
   - Error response: 403 Forbidden if the order belongs to a different 
     user (a user must never view another user's order details)

---

Business logic notes:
- Stock must be decremented at the moment of checkout (not when the item 
  was added to the cart), since this task treats checkout as the point of 
  commitment.
- Order cancellation is explicitly out of scope — do not implement any 
  cancel/refund/restock endpoint at this stage.
- Since there's no real payment gateway, treat the payment step as always 
  succeeding after the simulated delay (no retry logic needed).
- Use a database transaction around the entire checkout logic (steps c–h) 
  so that a failure partway through (e.g. stock check fails on the 3rd 
  item) doesn't leave a partial order or partially-decremented stock.

Error handling:
- 400 Bad Request → "Cart is empty" / "Insufficient stock for {product}"
- 401 Unauthorized → returned automatically by [Authorize] if no/invalid token
- 403 Forbidden → attempting to view another user's order
- 404 Not Found → order not found
- 500 Internal Server Error → "Something went wrong" (transaction should 
  roll back automatically)
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```
- [ ] A logged-in user can check out their cart and receive a confirmed order.
- [ ] Product stock decreases immediately upon successful checkout.
- [ ] Checkout fails cleanly (no partial order, no stock changes) if any 
      product in the cart is out of stock or inactive.
- [ ] The cart is emptied after a successful checkout.
- [ ] Order items store a snapshot of product name and price at purchase 
      time — unaffected by later price changes.
- [ ] A user can view a list of their own past orders.
- [ ] A user can view full details of one of their own orders.
- [ ] A user cannot view another user's order (403 Forbidden).
- [ ] No cancellation endpoint exists at this stage.
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

- **Blocked by / related ids:** (tracker ids only; optional short note)
- **Depends on code areas or other stories:**

## Extra notes (optional)

- Anything not captured above (e.g. chat context) — keep short.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `c#`.

## Out of scope

- What this story explicitly does **not** cover:
