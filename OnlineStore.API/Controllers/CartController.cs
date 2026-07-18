using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.API.Data;
using OnlineStore.API.Dtos;
using OnlineStore.API.Entities;

namespace OnlineStore.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;

    public CartController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddToCartRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var product = await _db.Products.FindAsync(request.ProductId);
            if (product is null || !product.IsActive)
            {
                return NotFound(new { message = "Product not found" });
            }

            var existing = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == request.ProductId);

            // Validate stock against the resulting total, not just the delta.
            var newQuantity = (existing?.Quantity ?? 0) + request.Quantity;
            if (newQuantity > product.Stock)
            {
                return BadRequest(new { message = "Requested quantity exceeds available stock" });
            }

            CartItem item;
            if (existing is null)
            {
                item = new CartItem
                {
                    UserId = userId.Value,
                    ProductId = product.Id,
                    Quantity = request.Quantity
                };
                _db.CartItems.Add(item);
            }
            else
            {
                existing.Quantity = newQuantity;
                item = existing;
            }

            await _db.SaveChangesAsync();

            return Ok(ToDto(item, product));
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

            var items = await _db.CartItems
                .Where(ci => ci.UserId == userId)
                .OrderBy(ci => ci.Id)
                .Select(ci => new CartItemDto(
                    ci.Id,
                    ci.ProductId,
                    ci.Product!.Name,
                    ci.Product.Price,
                    ci.Quantity,
                    ci.Product.Price * ci.Quantity))
                .ToListAsync();

            var total = items.Sum(i => i.Subtotal);

            return Ok(new CartResponse(items, total));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var item = await _db.CartItems.FindAsync(id);
            if (item is null)
            {
                return NotFound(new { message = "Cart item not found" });
            }

            // A user must never be able to delete another user's cart item.
            if (item.UserId != userId)
            {
                return Forbid();
            }

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();

            return NoContent();
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

    private static CartItemDto ToDto(CartItem item, Product product) =>
        new(item.Id, product.Id, product.Name, product.Price, item.Quantity, product.Price * item.Quantity);
}
