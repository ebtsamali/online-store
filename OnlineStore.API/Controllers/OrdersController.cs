using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.API.Data;
using OnlineStore.API.Dtos;
using OnlineStore.API.Entities;

namespace OnlineStore.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var cartItems = await _db.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userId)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                return BadRequest(new { message = "Cart is empty" });
            }

            // Validate every line against live product state BEFORE writing anything,
            // so a failure on any line creates no order and changes no stock.
            foreach (var ci in cartItems)
            {
                if (ci.Product is null || !ci.Product.IsActive)
                {
                    return BadRequest(new { message = $"Product {ci.ProductId} is unavailable" });
                }

                if (ci.Quantity > ci.Product.Stock)
                {
                    return BadRequest(new { message = $"Insufficient stock for {ci.Product.Name}" });
                }
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            // Snapshot name/price into the order items at the moment of checkout.
            var order = new Order
            {
                UserId = userId.Value,
                Status = "pending",
                Items = cartItems.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product!.Name,
                    UnitPrice = ci.Product.Price,
                    Quantity = ci.Quantity
                }).ToList()
            };
            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            // Commit stock at checkout (the point of commitment).
            foreach (var ci in cartItems)
            {
                ci.Product!.Stock -= ci.Quantity;
            }

            _db.Orders.Add(order);

            await Task.Delay(1500); // simulate a payment-gateway call
            order.Status = "paid";  // simulation always succeeds

            _db.CartItems.RemoveRange(cartItems);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var dto = ToDetail(order);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, dto);
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderSummaryDto(o.Id, o.Status, o.Total, o.CreatedAt))
                .ToListAsync();

            return Ok(orders);
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // A user must never view another user's order.
            if (order.UserId != userId)
            {
                return Forbid();
            }

            return Ok(ToDetail(order));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    private int? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : null;
    }

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
}
