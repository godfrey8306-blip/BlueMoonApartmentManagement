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
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _tokenService;

    public AuthController(
        AppDbContext context,
        PasswordHasher passwordHasher,
        JwtTokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var username = request.Username.Trim();
        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.Username == username);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        var (token, expiresAtUtc) = _tokenService.CreateToken(user);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            User = ToResponse(user)
        });
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
