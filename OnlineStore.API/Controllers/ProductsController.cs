using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.API.Data;
using OnlineStore.API.Dtos;
using OnlineStore.API.Entities;
using OnlineStore.API.Services;

namespace OnlineStore.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Roles = "admin")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IImageStorageService _images;

    public ProductsController(AppDbContext db, IImageStorageService images)
    {
        _db = db;
        _images = images;
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
                .Select(p => new ProductListItemDto(p.Id, p.Name, p.Price, p.Stock, p.IsActive, p.ImageUrl))
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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateProductFormRequest request)
    {
        try
        {
            try { _images.Validate(request.Image); }
            catch (InvalidImageException ex) { return BadRequest(new { message = ex.Message }); }

            if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            {
                return BadRequest(new { message = "Category not found" });
            }

            if (!await _db.Brands.AnyAsync(b => b.Id == request.BrandId))
            {
                return BadRequest(new { message = "Brand not found" });
            }

            // Write the file only after all other validation passes.
            var imageUrl = await _images.SaveAsync(request.Image);

            // Server owns IsActive and CreatedAt (entity defaults) — never trust the client.
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                CategoryId = request.CategoryId,
                BrandId = request.BrandId,
                ImageUrl = imageUrl
            };

            _db.Products.Add(product);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch
            {
                _images.Delete(imageUrl); // rollback the orphaned file
                throw;
            }

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDetail(product));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateProductFormRequest request)
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

            string? newImageUrl = null;
            var oldImageUrl = product.ImageUrl;

            if (request.Image is not null && request.Image.Length > 0)
            {
                try { _images.Validate(request.Image); }
                catch (InvalidImageException ex) { return BadRequest(new { message = ex.Message }); }

                newImageUrl = await _images.SaveAsync(request.Image);
                product.ImageUrl = newImageUrl;
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.Stock = request.Stock;
            product.CategoryId = request.CategoryId;
            product.BrandId = request.BrandId;
            product.IsActive = request.IsActive;
            // CreatedAt is intentionally left unchanged.

            try
            {
                await _db.SaveChangesAsync();
            }
            catch
            {
                if (newImageUrl is not null) _images.Delete(newImageUrl); // rollback the new file
                throw;
            }

            // Only after a successful save, delete the replaced file.
            if (newImageUrl is not null && !string.IsNullOrEmpty(oldImageUrl))
                _images.Delete(oldImageUrl);

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

            var imageUrl = product.ImageUrl;

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            // Remove the image file only after the row is gone (prevents orphaned files).
            if (!string.IsNullOrEmpty(imageUrl)) _images.Delete(imageUrl);

            return NoContent();
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    private static ProductDetailDto ToDetail(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CategoryId, p.BrandId, p.IsActive, p.CreatedAt, p.ImageUrl);
}
