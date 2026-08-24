using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.API.Data;
using OnlineStore.API.Dtos;
using OnlineStore.API.Entities;

namespace OnlineStore.API.Controllers;

[ApiController]
[Route("api/brands")]
[Authorize(Roles = "admin")]
public class BrandsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BrandsController(AppDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var items = await _db.Brands
                .OrderBy(b => b.Name)
                .Select(b => new BrandDto(b.Id, b.Name))
                .ToListAsync();

            return Ok(items);
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BrandRequest request)
    {
        try
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            if (await _db.Brands.AnyAsync(b => EF.Functions.ILike(b.Name, name)))
            {
                return BadRequest(new { message = "A brand with this name already exists" });
            }

            var brand = new Brand { Name = name };
            _db.Brands.Add(brand);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(List), new BrandDto(brand.Id, brand.Name));
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BrandRequest request)
    {
        try
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand is null)
            {
                return NotFound(new { message = "Brand not found" });
            }

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            if (await _db.Brands.AnyAsync(b => b.Id != id && EF.Functions.ILike(b.Name, name)))
            {
                return BadRequest(new { message = "A brand with this name already exists" });
            }

            brand.Name = name;
            await _db.SaveChangesAsync();

            return Ok(new BrandDto(brand.Id, brand.Name));
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
            var brand = await _db.Brands.FindAsync(id);
            if (brand is null)
            {
                return NotFound(new { message = "Brand not found" });
            }

            if (await _db.Products.AnyAsync(p => p.BrandId == id))
            {
                return Conflict(new { message = "Cannot delete brand: it is still used by one or more products" });
            }

            _db.Brands.Remove(brand);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }
}
