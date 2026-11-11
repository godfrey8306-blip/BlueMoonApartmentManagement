using BlueMoon.API;
using BlueMoon.API.Data;
using BlueMoon.API.DTOs;
using BlueMoon.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueMoon.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;

    public UsersController(AppDbContext context, PasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? NotFound() : Ok(ToResponse(user));
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        var hasUsers = await _context.Users.AnyAsync();
        if (hasUsers && !User.IsInRole("Admin"))
            return Forbid();

        if (!hasUsers && !string.Equals(request.Role, "Admin", StringComparison.Ordinal))
            return BadRequest(new { message = "The first account must have the Admin role." });

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        if (await _context.Users.AnyAsync(u => u.Username == username || u.Email == email))
            return Conflict(new { message = "Username or email already exists." });

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Email = email,
            Role = request.Role,
            IsActive = true
        };

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Username or email already exists." });
        }

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ToResponse(user));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return NotFound();

        var email = request.Email.Trim();
        if (await _context.Users.AnyAsync(u => u.Email == email && u.Id != id))
            return Conflict(new { message = "Email already exists." });

        if (user.IsActive && user.Role == "Admin" && request.Role != "Admin")
        {
            var activeAdminCount = await _context.Users.CountAsync(u =>
                u.IsActive && u.Role == "Admin" && u.Id != id);
            if (activeAdminCount == 0)
                return Conflict(new { message = "At least one active Admin account is required." });
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Role = request.Role;
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Email already exists." });
        }

        return Ok(ToResponse(user));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/lock")]
    public async Task<IActionResult> Lock(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return NotFound();

        if (string.Equals(User.Identity?.Name, user.Username, StringComparison.Ordinal))
            return Conflict(new { message = "You cannot lock your own account." });

        if (user.IsActive && user.Role == "Admin")
        {
            var activeAdminCount = await _context.Users.CountAsync(u =>
                u.IsActive && u.Role == "Admin" && u.Id != id);
            if (activeAdminCount == 0)
                return Conflict(new { message = "At least one active Admin account is required." });
        }

        user.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive
    };
}
