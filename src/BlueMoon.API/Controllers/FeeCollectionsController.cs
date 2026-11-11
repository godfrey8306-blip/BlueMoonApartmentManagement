using System.Security.Claims;
using BlueMoon.API.Data;
using BlueMoon.API.DTOs;
using BlueMoon.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueMoon.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public sealed class FeeCollectionsController : ControllerBase
{
    private static readonly string[] ValidStatuses = ["Open", "Closed", "Cancelled"];
    private readonly AppDbContext _context;

    public FeeCollectionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FeeCollectionResponseDto>>> GetAll(
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] string? status)
    {
        var query = FeeCollectionResponses();

        if (month.HasValue)
            query = query.Where(c => c.Month == month.Value);

        if (year.HasValue)
            query = query.Where(c => c.Year == year.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status.Trim());

        return Ok(await query
            .OrderByDescending(c => c.Year)
            .ThenByDescending(c => c.Month)
            .ThenBy(c => c.Name)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FeeCollectionResponseDto>> GetById(int id)
    {
        var collection = await FeeCollectionResponses()
            .FirstOrDefaultAsync(c => c.Id == id);

        return collection is null ? NotFound() : Ok(collection);
    }

    [HttpPost]
    public async Task<ActionResult<FeeCollectionResponseDto>> Create(FeeCollectionCreateDto dto)
    {
        var creatorId = await GetActiveAdminId();
        if (!creatorId.HasValue)
            return Unauthorized(new { message = "Authenticated active Admin user is required." });

        if (await HasDuplicatePeriod(dto.Month, dto.Year))
            return Conflict(new { message = "A fee collection already exists for this month and year." });

        var collection = new FeeCollection
        {
            Name = dto.Name.Trim(),
            Month = dto.Month,
            Year = dto.Year,
            CreatedByUserId = creatorId.Value,
            Status = "Open"
        };

        _context.FeeCollections.Add(collection);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The fee collection could not be saved." });
        }

        var response = await FeeCollectionResponses()
            .FirstAsync(c => c.Id == collection.Id);

        return CreatedAtAction(nameof(GetById), new { id = collection.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, FeeCollectionUpdateDto dto)
    {
        var collection = await _context.FeeCollections.FirstOrDefaultAsync(c => c.Id == id);
        if (collection is null)
            return NotFound();

        if (await HasDuplicatePeriod(dto.Month, dto.Year, id))
            return Conflict(new { message = "A fee collection already exists for this month and year." });

        var hasCharges = await _context.FeeCharges.AnyAsync(c => c.FeeCollectionId == id);
        if (hasCharges && (collection.Month != dto.Month || collection.Year != dto.Year))
        {
            return Conflict(new { message = "The period cannot be changed after fee charges have been created." });
        }

        collection.Name = dto.Name.Trim();
        collection.Month = dto.Month;
        collection.Year = dto.Year;
        collection.Status = dto.Status;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The fee collection could not be updated." });
        }

        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, FeeCollectionStatusDto dto)
    {
        var collection = await _context.FeeCollections.FirstOrDefaultAsync(c => c.Id == id);
        if (collection is null)
            return NotFound();

        collection.Status = dto.Status;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var collection = await _context.FeeCollections.FirstOrDefaultAsync(c => c.Id == id);
        if (collection is null)
            return NotFound();

        if (await _context.FeeCharges.AnyAsync(c => c.FeeCollectionId == id))
            return Conflict(new { message = "Cannot delete a fee collection referenced by fee charges." });

        _context.FeeCollections.Remove(collection);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Cannot delete the fee collection because related data exists." });
        }

        return NoContent();
    }

    private IQueryable<FeeCollectionResponseDto> FeeCollectionResponses()
    {
        return _context.FeeCollections
            .AsNoTracking()
            .Select(c => new FeeCollectionResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Month = c.Month,
                Year = c.Year,
                CreatedAt = c.CreatedAt,
                CreatedByUserId = c.CreatedByUserId,
                CreatedByUsername = _context.Users
                    .Where(u => u.Id == c.CreatedByUserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? string.Empty,
                Status = c.Status
            });
    }

    private Task<bool> HasDuplicatePeriod(int month, int year, int? excludedId = null)
    {
        var query = _context.FeeCollections
            .Where(c => c.Month == month && c.Year == year);

        if (excludedId.HasValue)
            query = query.Where(c => c.Id != excludedId.Value);

        return query.AnyAsync();
    }

    private async Task<int?> GetActiveAdminId()
    {
        var subject = User.FindFirstValue("sub") ??
                      User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(subject, out var userId))
            return null;

        return await _context.Users
            .Where(u => u.Id == userId && u.IsActive && u.Role == "Admin")
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync();
    }
}
