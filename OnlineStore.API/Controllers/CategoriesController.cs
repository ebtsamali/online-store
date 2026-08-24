using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.API.Data;
using OnlineStore.API.Dtos;
using OnlineStore.API.Entities;

namespace OnlineStore.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize(Roles = "admin")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var items = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto(c.Id, c.Name))
                .ToListAsync();

            return Ok(items);
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryRequest request)
    {
        try
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            if (await _db.Categories.AnyAsync(c => EF.Functions.ILike(c.Name, name)))
            {
                return BadRequest(new { message = "A category with this name already exists" });
            }

            var category = new Category { Name = name };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(List), new CategoryDto(category.Id, category.Name));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
    {
        try
        {
            var category = await _db.Categories.FindAsync(id);
            if (category is null)
            {
                return NotFound(new { message = "Category not found" });
            }

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            if (await _db.Categories.AnyAsync(c => c.Id != id && EF.Functions.ILike(c.Name, name)))
            {
                return BadRequest(new { message = "A category with this name already exists" });
            }

            category.Name = name;
            await _db.SaveChangesAsync();

            return Ok(new CategoryDto(category.Id, category.Name));
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
            var category = await _db.Categories.FindAsync(id);
            if (category is null)
            {
                return NotFound(new { message = "Category not found" });
            }

            // Pre-check the Restrict FK instead of letting SaveChangesAsync throw a
            // DbUpdateException — turns an unhandled 500 into a clean 409.
            if (await _db.Products.AnyAsync(p => p.CategoryId == id))
            {
                return Conflict(new { message = "Cannot delete category: it is still used by one or more products" });
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }
}
