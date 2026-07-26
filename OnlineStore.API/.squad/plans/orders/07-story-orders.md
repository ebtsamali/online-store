# Story 07 — Order Checkout, Payment Simulation & Order History

Turn a logged-in customer's cart into a confirmed **Order**: validate stock, snapshot product name/price into **OrderItem** rows, decrement `Product.Stock`, simulate a payment, empty the cart, and expose read-only order history. All checkout side effects run inside **one database transaction** so a mid-way failure leaves no partial order and no partially-decremented stock. This story introduces the **`Order`** and **`OrderItem`** entities (neither exists yet) and ships one EF migration for both tables.

---

## Prerequisites

- **Story 05 completed** ([`../cart/05-story-cart-crud.md`](../cart/05-story-cart-crud.md)): the `CartItem` entity, `CartItems` `DbSet`, and `CartController` exist. Checkout **reads and then clears** the caller's `CartItems`. Verified: `CartItem` has `UserId`, `ProductId`, `Quantity`, and a nullable `Product` navigation ([`Entities/CartItem.cs`](../../../Entities/CartItem.cs) lines 3–13); the `CartItems` `DbSet` is registered in [`Data/AppDbContext.cs`](../../../Data/AppDbContext.cs) line 16.
- **Story 04 completed** ([`../products/04-story-products-crud.md`](../products/04-story-products-crud.md)): the `Product` entity and `Products` `DbSet` exist. Checkout reads `Product.Name`, `Product.Price`, `Product.Stock`, `Product.IsActive` and writes `Product.Stock` ([`Entities/Product.cs`](../../../Entities/Product.cs) lines 3–19).
- **Story 03 completed** ([`../middleware/03-authorization-middleware.md`](../middleware/03-authorization-middleware.md)): the JWT pipeline, `AddAuthorization()`, and the Swagger **Authorize** button are already wired in [`Program.cs`](../../../Program.cs) (verified: `AddAuthentication().AddJwtBearer` lines 54–68, `AddAuthorization()` line 70, `UseAuthentication()`→`UseAuthorization()`→`MapControllers()` lines 88–92). **Do not** re-add any auth wiring — only apply `[Authorize]` attributes.
- **User-id claim shape (verified):** [`Services/TokenService.cs`](../../../Services/TokenService.cs) line 31 issues the user id as `JwtRegisteredClaimNames.Sub`; the bearer handler maps `sub` → `ClaimTypes.NameIdentifier` inbound. Read it with `User.FindFirstValue(ClaimTypes.NameIdentifier)` + `int.TryParse` — the exact precedent is [`Controllers/CartController.cs`](../../../Controllers/CartController.cs) `GetUserId()` (lines 144–148) and [`Controllers/AuthController.cs`](../../../Controllers/AuthController.cs) `Me()` (lines 83–94). Never trust a `UserId` sent in the request body.
- Confirmed already available in [`OnlineStore.API.csproj`](../../../OnlineStore.API.csproj): `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.3), `Microsoft.EntityFrameworkCore.Design`/`.Tools` (10.0.9). **No new packages required.** Target framework is `net10.0`.
- PostgreSQL must be reachable at `ConnectionStrings:DefaultConnection` ([`appsettings.json`](../../../appsettings.json)) for the migration and manual verification. Npgsql supports real `BeginTransactionAsync` transactions — this story relies on that.
- **Admin not required:** every order endpoint is `[Authorize]` for **any** logged-in user (customer role is enough), mirroring the cart in Story 05. No user-promotion step is needed to test.

---

## Story Goal

Deliver a per-user order/checkout API backed by two new tables (`Orders`, `OrderItems`):

1. `POST /api/orders/checkout` — **any logged-in user**. Converts the caller's cart into a confirmed order inside a single transaction: validates every cart line against live product state, creates the `Order` + `OrderItem`s (snapshotting name/price), decrements `Product.Stock`, simulates payment (short delay, always succeeds → `Status = "paid"`), clears the cart, and returns the created `OrderDetailDto` as **201 Created**.
2. `GET /api/orders` — **any logged-in user**. Returns the caller's own orders, most-recent-first, as `List<OrderSummaryDto>`.
3. `GET /api/orders/{id}` — **any logged-in user**. Returns one order (with its items) as `OrderDetailDto`; **404** if it does not exist, **403** if it belongs to another user.

**Not in scope:** order cancellation / refund / restock (explicitly excluded by the intake — do **not** add any cancel endpoint); a real payment gateway or a payment-failure path (payment always succeeds after the simulated delay); reserving stock at add-to-cart time (stock is committed only at checkout); editing or deleting orders; admin views across all users' orders. These are possible follow-up stories.

---

## Context — Read These Files First

1. [`Controllers/CartController.cs`](../../../Controllers/CartController.cs) — 152 lines. The **primary precedent** for this story's controller. Copy verbatim: the `[ApiController]` + `[Route("api/…")]` + controller-level `[Authorize]` (lines 11–14), constructor-injected `AppDbContext _db` (lines 16–21), per-action `try/catch` → `StatusCode(500, new { message = "Something went wrong" })`, `GetUserId()` helper (lines 144–148), the `userId is null → Unauthorized()` guard, `NotFound(new { message = "…" })` / `BadRequest(new { message = "…" })` shapes, and `Forbid()` for a cross-user access attempt (line 130). The cart's `GET` projection (lines 88–98) shows the `ci.Product!.Name` JOIN-in-`Select` pattern you reuse for order items.
2. [`Entities/CartItem.cs`](../../../Entities/CartItem.cs) — 13 lines. Checkout loads the caller's `CartItems` (with `Product`), converts each to an `OrderItem`, then removes them all. Fields read: `ProductId`, `Quantity`, and the `Product` navigation.
3. [`Entities/Product.cs`](../../../Entities/Product.cs) — 19 lines. Checkout reads `Name`, `Price` (`decimal`), `Stock` (`int`), `IsActive` (`bool`) and **writes** `Stock`. Mirror the entity style for the new entities (namespace `OnlineStore.API.Entities`, `string` props `= string.Empty`, `CreatedAt = DateTime.UtcNow`).
4. [`Entities/User.cs`](../../../Entities/User.cs) — 11 lines. `Order.UserId` FKs to `User.Id`.
5. [`Data/AppDbContext.cs`](../../../Data/AppDbContext.cs) — 48 lines. `DbSet`s at lines 12–16; `OnModelCreating` (lines 18–47) configures the unique `User.Email` index, the two `Product`→`Category`/`Brand` FKs with **`DeleteBehavior.Restrict`** (lines 25–35), and the two `CartItem` FKs with **`DeleteBehavior.Cascade`** (lines 37–47). You add two `DbSet`s and three FK relationships here, following these same patterns (task 3). **Note the deliberate Restrict-vs-Cascade split** — you will make the same kind of deliberate choice for `OrderItem`→`Product` (see task 3 and Edge Cases).
6. [`Controllers/ProductsController.cs`](../../../Controllers/ProductsController.cs) — 241 lines. The `Create` action's **image-rollback try/catch around `SaveChangesAsync`** (lines 128–136) is the local precedent for "undo a side effect when the DB write fails"; the transaction in this story is the more robust equivalent for multi-step writes. Also note `FindAsync` usage and the `CreatedAtAction(nameof(GetById), …)` return (line 138) — mirror it for the checkout response.
7. [`Dtos/CartItemDto.cs`](../../../Dtos/CartItemDto.cs) / [`Dtos/CartResponse.cs`](../../../Dtos/CartResponse.cs) / [`Dtos/ProductDetailDto.cs`](../../../Dtos/ProductDetailDto.cs) — DTO style: positional `record`s, one per file, namespace `OnlineStore.API.Dtos`. Mirror this exact form for the order DTOs (task 4). **No request DTO is needed** — checkout takes an empty body.
8. [`Migrations/20260718163616_AddCartItem.cs`](../../../Migrations/20260718163616_AddCartItem.cs) — the `AddCartItem` migration `Up`: the exact Npgsql shape this repo generates for a new table with FKs (`Id` identity via `NpgsqlValueGenerationStrategy.IdentityByDefaultColumn`, `integer` columns, `timestamp with time zone` for `DateTime`, `CreateIndex` on each FK column, `AddForeignKey` with `onDelete`). Your new migration is **auto-generated** and should match this shape. **Do not hand-write it.** The current head migration is `20260719112746_AddProductImageUrl` — that is the migration you roll back **to** if needed.
9. [`Program.cs`](../../../Program.cs) — CORS `"Frontend"` policy for `http://localhost:3000` (lines 42–48, `UseCors` line 86) and the auth pipeline (lines 88–92) are already correct. **No change needed.**
10. [`Properties/launchSettings.json`](../../../Properties/launchSettings.json) — API at `http://localhost:5016`; Swagger at `/swagger`. Use for manual verification.

---

## Backend Tasks

`No frontend changes required.` This is an API-only story.

### 1 — Create the `Order` entity

**Create file: `Entities/Order.cs`**

```csharp
namespace OnlineStore.API.Entities;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "pending"; // "pending" | "paid" | "failed"
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
```

> `Status` is a plain `string` with a default of `"pending"` (matches the intake and the `string Role = "customer"` precedent in `User`). The `"failed"` value exists in the domain but is **not** produced by this story (payment always succeeds — see task 5g and Edge Cases). Navigations are nullable / initialized so the executor is not forced to always `Include` them.

### 2 — Create the `OrderItem` entity

**Create file: `Entities/OrderItem.cs`**

```csharp
namespace OnlineStore.API.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty; // snapshot at checkout
    public decimal UnitPrice { get; set; }                   // snapshot at checkout
    public int Quantity { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
}
```

> **`ProductName` and `UnitPrice` are snapshots** copied from the `Product` at the moment of checkout. Never re-read `Product.Name` / `Product.Price` when returning order history — the whole point is that a later price/name change must not alter past orders.

### 3 — Register `DbSet`s and relationships — `Data/AppDbContext.cs`

**File: `Data/AppDbContext.cs`**

Add after the `CartItems` `DbSet` (line 16):

```csharp
public DbSet<Order> Orders => Set<Order>();
public DbSet<OrderItem> OrderItems => Set<OrderItem>();
```

Extend `OnModelCreating` (keep everything already there). Add three FK relationships:

```csharp
modelBuilder.Entity<Order>()
    .HasOne(o => o.User)
    .WithMany()
    .HasForeignKey(o => o.UserId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<OrderItem>()
    .HasOne(oi => oi.Order)
    .WithMany(o => o.Items)
    .HasForeignKey(oi => oi.OrderId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<OrderItem>()
    .HasOne(oi => oi.Product)
    .WithMany()
    .HasForeignKey(oi => oi.ProductId)
    .OnDelete(DeleteBehavior.Restrict);
```

> **Deliberate delete-behavior choices — flag these in the PR:**
> - `Order`→`User` **Cascade**: deleting a user removes their order history (same disposable-per-user reasoning as `CartItem`→`User` in Story 05).
> - `OrderItem`→`Order` **Cascade**: order items are part of the order aggregate; deleting the parent order removes its lines.
> - `OrderItem`→`Product` **`Restrict`** (like `Product`→`Category`/`Brand`, **not** like `CartItem`→`Product`). This **preserves order history**: a product that appears in any order can no longer be **hard-deleted** — a deliberate difference from the cart, whose rows are disposable. **Consequence (verified regression risk):** [`ProductsController.Delete`](../../../Controllers/ProductsController.cs) (lines 211–236) is a **hard delete**; attempting to delete a product referenced by an `OrderItem` will now throw `DbUpdateException` → caught → **500** `"Something went wrong"`. This is intentional for order integrity — the correct way to retire a product that has been ordered is to set `IsActive = false` (product soft-retire already exists via `Product.IsActive`). See Edge Cases; note the alternative (nullable `ProductId` + `SetNull`) as a possible follow-up.

### 4 — Create the DTOs

**Create file: `Dtos/OrderItemDto.cs`**

```csharp
namespace OnlineStore.API.Dtos;

public record OrderItemDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);
```

**Create file: `Dtos/OrderSummaryDto.cs`**

```csharp
namespace OnlineStore.API.Dtos;

public record OrderSummaryDto(
    int Id,
    string Status,
    decimal Total,
    DateTime CreatedAt);
```

**Create file: `Dtos/OrderDetailDto.cs`**

```csharp
namespace OnlineStore.API.Dtos;

public record OrderDetailDto(
    int Id,
    string Status,
    decimal Total,
    DateTime CreatedAt,
    List<OrderItemDto> Items);
```

> No `CheckoutRequest` DTO — the checkout action takes **no body** (the cart is resolved from the token). `Subtotal = UnitPrice * Quantity`; `Total = sum of subtotals`.

### 5 — Create `Controllers/OrdersController.cs`

**Create file: `Controllers/OrdersController.cs`**

- Namespace `OnlineStore.API.Controllers`; `[ApiController]`, `[Route("api/orders")]`, `[Authorize]` at the **controller level** (every action requires a valid token; no `[AllowAnonymous]`).
- Constructor-inject `AppDbContext _db` (mirror `CartController` lines 16–21).
- Usings: `using System.Security.Claims;`, `using Microsoft.AspNetCore.Authorization;`, `using Microsoft.AspNetCore.Mvc;`, `using Microsoft.EntityFrameworkCore;`, `using OnlineStore.API.Data;`, `using OnlineStore.API.Dtos;`, `using OnlineStore.API.Entities;`.
- Copy the `GetUserId()` helper verbatim from `CartController` (lines 144–148):

```csharp
private int? GetUserId()
{
    var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return int.TryParse(raw, out var id) ? id : null;
}
```

#### `POST /api/orders/checkout` — `[HttpPost("checkout")]`

Signature: `public async Task<IActionResult> Checkout()` (no body).

1. Wrap the whole body in `try/catch` → `StatusCode(500, new { message = "Something went wrong" })`.
2. `var userId = GetUserId(); if (userId is null) return Unauthorized();`
3. Load the caller's cart **with products**, so stock/name/price are available:
   ```csharp
   var cartItems = await _db.CartItems
       .Include(ci => ci.Product)
       .Where(ci => ci.UserId == userId)
       .ToListAsync();
   ```
4. **Empty cart → 400:** `if (cartItems.Count == 0) return BadRequest(new { message = "Cart is empty" });`
5. **Validate every line against live product state** *before* writing anything. For each `ci` in `cartItems`:
   - If `ci.Product is null || !ci.Product.IsActive` → `return BadRequest(new { message = $"Product {ci.ProductId} is unavailable" });`
   - If `ci.Quantity > ci.Product.Stock` → `return BadRequest(new { message = $"Insufficient stock for {ci.Product.Name}" });`
   - Returning here (before creating the order) satisfies "do NOT create the order" on any failed check.
6. **Open a transaction** covering the create + stock decrement + cart clear:
   ```csharp
   await using var tx = await _db.Database.BeginTransactionAsync();
   ```
7. Build the order, snapshotting name/price and decrementing stock in the same pass:
   ```csharp
   var order = new Order
   {
       UserId = userId.Value,
       Status = "pending",
       Items = cartItems.Select(ci => new OrderItem
       {
           ProductId = ci.ProductId,
           ProductName = ci.Product!.Name,   // snapshot
           UnitPrice = ci.Product.Price,     // snapshot
           Quantity = ci.Quantity
       }).ToList()
   };
   order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

   foreach (var ci in cartItems)
       ci.Product!.Stock -= ci.Quantity;   // commit stock at checkout

   _db.Orders.Add(order);
   ```
8. **Simulate payment**, then mark paid, then empty the cart:
   ```csharp
   await Task.Delay(1500);       // simulate a payment-gateway call
   order.Status = "paid";        // simulation always succeeds
   _db.CartItems.RemoveRange(cartItems);

   await _db.SaveChangesAsync();
   await tx.CommitAsync();
   ```
   > A single `SaveChangesAsync` persists the new order + items, the decremented stock, and the cart deletions atomically; `CommitAsync` finalizes. If anything throws before the commit, the `catch` returns 500 and the transaction is disposed **without** committing → full rollback (no partial order, no lost stock, cart intact). Do **not** put `Task.Delay` outside the try/catch.
9. Return **201** with the created order:
   ```csharp
   var dto = ToDetail(order);
   return CreatedAtAction(nameof(GetById), new { id = order.Id }, dto);
   ```

#### `GET /api/orders` — `[HttpGet]`

Signature: `public async Task<IActionResult> Get()`

1. `try/catch` → 500. `var userId = GetUserId(); if (userId is null) return Unauthorized();`
2. Caller's orders, most-recent-first, projected to summaries:
   ```csharp
   var orders = await _db.Orders
       .Where(o => o.UserId == userId)
       .OrderByDescending(o => o.CreatedAt)
       .Select(o => new OrderSummaryDto(o.Id, o.Status, o.Total, o.CreatedAt))
       .ToListAsync();
   return Ok(orders);
   ```
   > Filtering by `o.UserId == userId` guarantees a user never sees another user's orders.

#### `GET /api/orders/{id}` — `[HttpGet("{id:int}")]`

Signature: `public async Task<IActionResult> GetById(int id)`

1. `try/catch` → 500. `var userId = GetUserId(); if (userId is null) return Unauthorized();`
2. Load the order **with items**:
   ```csharp
   var order = await _db.Orders
       .Include(o => o.Items)
       .FirstOrDefaultAsync(o => o.Id == id);
   ```
3. If `null` → `return NotFound(new { message = "Order not found" });`
4. **Ownership check:** `if (order.UserId != userId) return Forbid();` (**403** — a user must never view another user's order).
5. `return Ok(ToDetail(order));`

#### Private mapper

```csharp
private static OrderDetailDto ToDetail(Order order) =>
    new(
        order.Id,
        order.Status,
        order.Total,
        order.CreatedAt,
        order.Items
            .Select(i => new OrderItemDto(
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.UnitPrice * i.Quantity))
            .ToList());
```

> **Never return the `Order`/`OrderItem` entity directly** — always project to a DTO so `UserId` and navigation graphs are not serialized. `ToDetail` reads only the **snapshot** fields (`ProductName`, `UnitPrice`), never the live `Product`.

### 6 — Generate and apply the EF migration

From `OnlineStore.API/`:

```powershell
dotnet ef migrations add AddOrders
dotnet ef database update
```

Inspect the generated `Migrations/*_AddOrders.cs`: it should `CreateTable("Orders", …)` (`Id` identity, `UserId` `integer`, `Status` `text`, `Total` `numeric`, `CreatedAt` `timestamp with time zone`) and `CreateTable("OrderItems", …)` (`Id` identity, `OrderId`/`ProductId` `integer`, `ProductName` `text`, `UnitPrice` `numeric`, `Quantity` `integer`), plus `CreateIndex` on `Orders.UserId`, `OrderItems.OrderId`, `OrderItems.ProductId`, and three `AddForeignKey` constraints — `onDelete: ReferentialAction.Cascade` for `Orders_Users` and `OrderItems_Orders`, and `onDelete: ReferentialAction.Restrict` for `OrderItems_Products`. **Do not hand-write it.** If `dotnet ef` is missing: `dotnet tool install --global dotnet-ef`.

---

## Edge Cases & Failure Modes

- **Empty cart:** `POST /api/orders/checkout` with no cart items → **400** `"Cart is empty"` (task 5 step 4), before any order row is created.
- **Inactive / deleted product in cart at checkout:** a product that went inactive (or was removed) after being added to the cart → **400** `"Product {id} is unavailable"` (task 5 step 5); no order created. Enforced by the `ci.Product is null || !ci.Product.IsActive` check.
- **Insufficient stock at checkout:** cart line quantity exceeds current `Product.Stock` → **400** `"Insufficient stock for {name}"` (task 5 step 5). Because validation runs over **all** lines before the transaction opens, a failure on the 3rd item leaves the first two products' stock **untouched** — satisfying "no partial order, no partial stock change" (acceptance criterion 3).
- **Transaction atomicity:** create-order + stock-decrement + cart-clear are one `SaveChangesAsync` inside `BeginTransactionAsync`/`CommitAsync` (task 5 steps 6–8). Any exception before `CommitAsync` disposes the transaction uncommitted → complete rollback; the `catch` returns **500** `"Something went wrong"`. **The `Task.Delay` and both writes must be inside the `try`** so a failure after the delay still rolls back.
- **Concurrent checkout of the same product (oversell race, flagged):** two simultaneous checkouts can both pass the stock check on the same product before either commits, driving `Stock` negative. This story adds **no** row lock or optimistic-concurrency token (keeps it minimal, matches Story 05's stance). Follow-up options: a `SELECT … FOR UPDATE` on the products (`_db.Products.FromSql(...)`) inside the transaction, or a `[ConcurrencyCheck]`/`xmin` token on `Product`. **Out of scope, flagged.**
- **Payment never fails:** the simulation always sets `Status = "paid"` after the delay (task 5 step 8). The `"failed"` status value exists in the domain but is **never** produced here; no retry/rollback-on-payment-failure path is implemented (intake says so explicitly). If a failure path is added later, it must also roll back the stock decrement.
- **Snapshot integrity:** `OrderItemDto` is built only from `OrderItem.ProductName`/`UnitPrice` (task 5 mapper), never the live `Product`. Changing a product's price or name after checkout does **not** change historical orders (acceptance criterion 5).
- **Cross-user order access:** `GET /api/orders/{id}` for another user's order → **403** `Forbid()` (task 5 GetById step 4), not 404. **Info-leak tradeoff (flagged):** 403 reveals the id exists; 404-for-both would hide it. The intake explicitly asks for **403**; note the alternative in the PR if the team prefers 404-for-both. `GET /api/orders` (list) filters by `UserId`, so cross-user rows never appear there.
- **Non-existent order:** `GET /api/orders/{id}` for an unknown id → **404** `"Order not found"`.
- **Missing / unparseable `sub` claim:** `GetUserId()` returns `null` → **401** `Unauthorized()` on every action.
- **Hard-deleting an ordered product (regression, flagged):** with `OrderItem`→`Product` = `Restrict` (task 3), [`ProductsController.Delete`](../../../Controllers/ProductsController.cs) on a product referenced by any order throws `DbUpdateException` → caught → **500**. Intended (protects order history); the correct retirement path is `IsActive = false`. Flag for the product-endpoint owner; consider surfacing a friendlier **409 Conflict** there as a follow-up.
- **`decimal` precision:** `Price`/`UnitPrice`/`Total` map to Postgres `numeric` (unbounded); subtotals/total are computed in-memory from snapshots — no precision loss.

---

## Test Plan

No test project exists yet (noted in Stories 02–05). If/when one is added (e.g. `OnlineStore.API.Tests` with `WebApplicationFactory`), add:

1. **Checkout — 201 happy path** (`OrderCheckoutTests`): user with 2 cart lines (active products, sufficient stock) → **201**; `OrderDetailDto` with `status = "paid"`, `total = Σ(unitPrice*qty)`, one `OrderItem` per line with snapshotted name/price; one `Orders` row + matching `OrderItems`.
2. **Checkout — stock decremented:** each product's `Stock` decreases by the ordered quantity after checkout.
3. **Checkout — cart emptied:** `GET /api/cart` returns no items after a successful checkout.
4. **Checkout — 400 empty cart:** checkout with an empty cart → **400** `"Cart is empty"`; no order created.
5. **Checkout — 400 insufficient stock, no side effects:** cart of 3 lines where the 3rd exceeds stock → **400** `"Insufficient stock for …"`; **no** order row, **no** stock change on any product, cart unchanged (asserts rollback/pre-validation).
6. **Checkout — 400 inactive product:** a line whose product is `IsActive = false` → **400** `"… unavailable"`; no order.
7. **Checkout — snapshot immutability:** check out, then change the product's `Price` and `Name`; `GET /api/orders/{id}` still shows the original values.
8. **Checkout — 401 no token:** → **401**.
9. **List — only own orders:** seed orders for users A and B; `GET /api/orders` as A → only A's, ordered `CreatedAt` desc.
10. **GetById — 200 own order:** A retrieves A's order → **200** with items.
11. **GetById — 403 other user's order:** A retrieves B's order id → **403**; **404** for an unknown id.
12. **GetById — 401 no token:** → **401**.
13. **No cancellation endpoint:** assert there is no route that cancels/deletes an order (acceptance criterion 9).

Until a test project exists, the manual matrix in **Verification Steps** is the acceptance gate — record results in the PR. Reference the manual-verification style in [`../cart/05-story-cart-crud.md`](../cart/05-story-cart-crud.md).

---

## Migration / Rollback

- **Forward:** `dotnet ef migrations add AddOrders` → `dotnet ef database update`. Creates the `Orders` and `OrderItems` tables (+ FK indexes + FKs). No existing table is altered → **no backfill risk**.
- **Half-applied risk:** minimal — two brand-new tables with no external dependents. If `database update` fails midway, re-run it (DDL is idempotent per-migration).
- **Rollback:** `dotnet ef database update AddProductImageUrl` (migration `20260719112746_AddProductImageUrl` — the current head before this story) reverts, dropping `OrderItems` then `Orders`; then `dotnet ef migrations remove` deletes the `AddOrders` migration files. Rolling back discards all order history — back it up first if it matters.

---

## Verification Steps

1. **Backend builds:** `dotnet build` in `OnlineStore.API/` — succeeds, no warnings.
2. **Migration applied:** `dotnet ef database update` completes; `Orders` and `OrderItems` tables exist with the expected FK columns/indexes.
3. **Seed data:** ensure at least two active products exist with known `Stock` (use Story 06's `POST /api/products` as admin, or insert directly). Note their ids and current stock.
4. **Backend runs:** `dotnet run --launch-profile http`, open `http://localhost:5016/swagger`.
5. **Auth required:** `POST /api/orders/checkout` with no token → **401**; same for `GET /api/orders`.
6. **Log in & fill cart:** `POST /api/auth/login` as a customer, click Swagger **Authorize**, paste the token; `POST /api/cart` twice to add two products with known quantities.
7. **Checkout (201):** `POST /api/orders/checkout` → **201** after ~1.5s; body is `OrderDetailDto` with `status: "paid"`, correct `total`, and one item per cart line with snapshotted `productName`/`unitPrice`.
8. **Stock decremented:** `GET /api/products/{id}` for each ordered product → `stock` reduced by the ordered quantity.
9. **Cart emptied:** `GET /api/cart` → `items: []`.
10. **Empty cart (400):** `POST /api/orders/checkout` again (cart now empty) → **400** `"Cart is empty"`.
11. **Insufficient stock (400):** add a product with `quantity` > its stock is blocked at add time (Story 05); to exercise checkout-time rejection, reduce a product's stock (admin `PUT`) below a cart quantity, then checkout → **400** `"Insufficient stock for …"`, and confirm no new order and unchanged stock.
12. **Snapshot immutability:** admin `PUT /api/products/{id}` to change a product's price/name; `GET /api/orders/{orderId}` → still shows the original name/price.
13. **Order history (200):** `GET /api/orders` → most-recent-first list of only this user's orders.
14. **Ownership (403):** log in as a **second** user, checkout to get their order id; as the **first** user call `GET /api/orders/{secondUsersOrderId}` → **403**. Unknown id → **404**.
15. **CORS:** preflight from `http://localhost:3000` to `/api/orders` passes (existing `"Frontend"` policy).

---

## Done Criteria

- [ ] `Order` and `OrderItem` entities exist; `Orders`/`OrderItems` `DbSet`s registered; one migration (`AddOrders`) creates both tables with FK indexes/constraints (Cascade for `Order`→`User` and `OrderItem`→`Order`, Restrict for `OrderItem`→`Product`).
- [ ] `POST /api/orders/checkout` converts the **caller's** cart (resolved from the JWT, not the body) into an order and returns **201** `OrderDetailDto` with `status: "paid"`.
- [ ] Product stock decreases by the ordered quantity on successful checkout.
- [ ] Checkout runs in a single transaction: an empty cart, an inactive/missing product, or insufficient stock yields **400** with **no** order created and **no** stock change.
- [ ] The cart is emptied after a successful checkout.
- [ ] `OrderItem` stores a snapshot of product name and price; later product price/name changes do not affect past orders.
- [ ] `GET /api/orders` returns only the caller's orders, most-recent-first.
- [ ] `GET /api/orders/{id}` returns the caller's own order (**200**), **403** for another user's order, **404** for a non-existent order.
- [ ] Every order endpoint returns **401** without a valid token.
- [ ] Endpoints never expose the raw `Order`/`OrderItem` entities — always a DTO.
- [ ] **No** cancellation/refund/restock endpoint exists.
- [ ] `dotnet build` succeeds; migration applies cleanly.

---

**STOP HERE. Report to the user and wait for confirmation before proceeding to any follow-up story.**
