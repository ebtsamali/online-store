using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.API.Data;
using OnlineStore.API.Dtos;
using OnlineStore.API.Entities;

namespace OnlineStore.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                return Conflict(new { message = "Email already exists" });
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var response = new RegisterResponse("Account created successfully", user.Name, user.Email);
            return CreatedAtAction(nameof(Register), response);
        }
        catch (DbUpdateException)
        {
            // Unique-index violation from a concurrent registration with the same email.
            return Conflict(new { message = "Email already exists" });
        }
        catch
        {
            return StatusCode(500, new { message = "Something went wrong" });
        }
    }
}
