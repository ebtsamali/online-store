using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.API.Data;
using OnlineStore.API.Dtos;
using OnlineStore.API.Entities;

namespace OnlineStore.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Roles = "admin")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            IQueryable<Product> query = _db.Products;

            // Anonymous and non-admin callers only see active products.
            if (!User.IsInRole("admin"))
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductListItemDto(p.Id, p.Name, p.Price, p.Stock, p.IsActive))
                .ToListAsync();

            return Ok(new { items, page, pageSize, total });
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound(new { message = "Product not found" });
            }

            // Don't leak inactive products to anonymous / non-admin callers.
            if (!product.IsActive && !User.IsInRole("admin"))
            {
                return NotFound(new { message = "Product not found" });
            }

            return Ok(ToDetail(product));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        try
        {
            if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            {
                return BadRequest(new { message = "Category not found" });
            }

            if (!await _db.Brands.AnyAsync(b => b.Id == request.BrandId))
            {
                return BadRequest(new { message = "Brand not found" });
            }

            // Server owns IsActive and CreatedAt (entity defaults) — never trust the client.
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                CategoryId = request.CategoryId,
                BrandId = request.BrandId
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDetail(product));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request)
    {
        try
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound(new { message = "Product not found" });
            }

            if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            {
                return BadRequest(new { message = "Category not found" });
            }

            if (!await _db.Brands.AnyAsync(b => b.Id == request.BrandId))
            {
                return BadRequest(new { message = "Brand not found" });
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.Stock = request.Stock;
            product.CategoryId = request.CategoryId;
            product.BrandId = request.BrandId;
            product.IsActive = request.IsActive;
            // CreatedAt is intentionally left unchanged.

            await _db.SaveChangesAsync();

            return Ok(ToDetail(product));
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
            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound(new { message = "Product not found" });
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    private static ProductDetailDto ToDetail(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CategoryId, p.BrandId, p.IsActive, p.CreatedAt);
}
