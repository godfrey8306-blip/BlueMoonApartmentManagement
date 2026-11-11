using BlueMoon.API.Data;
using BlueMoon.API.DTOs;
using BlueMoon.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueMoon.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HouseholdsController : ControllerBase
{
    private readonly AppDbContext _context;

    public HouseholdsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HouseholdResponseDto>>> GetAll(
        [FromQuery] int? apartmentId,
        [FromQuery] bool? isActive,
        [FromQuery] string? householdCode)
    {
        var query = HouseholdResponses();

        if (apartmentId.HasValue)
            query = query.Where(h => h.ApartmentId == apartmentId.Value);

        if (isActive.HasValue)
            query = query.Where(h => h.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(householdCode))
        {
            var search = householdCode.Trim();
            query = query.Where(h => EF.Functions.ILike(h.HouseholdCode, $"%{search}%"));
        }

        return Ok(await query
            .OrderBy(h => h.HouseholdCode)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HouseholdResponseDto>> GetById(int id)
    {
        var household = await HouseholdResponses()
            .FirstOrDefaultAsync(h => h.Id == id);

        return household is null ? NotFound() : Ok(household);
    }

    [HttpPost]
    public async Task<ActionResult<HouseholdResponseDto>> Create(HouseholdCreateDto dto)
    {
        var householdCode = dto.HouseholdCode.Trim();

        if (!await _context.Apartments.AnyAsync(a => a.Id == dto.ApartmentId))
            return NotFound(new { message = "Apartment not found." });

        if (await _context.Households.AnyAsync(h => h.HouseholdCode == householdCode))
            return Conflict(new { message = "Household code already exists." });

        var household = new Household
        {
            HouseholdCode = householdCode,
            ApartmentId = dto.ApartmentId,
            IsActive = true
        };

        _context.Households.Add(household);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Household code already exists or the apartment reference is invalid." });
        }

        var response = await HouseholdResponses()
            .FirstAsync(h => h.Id == household.Id);

        return CreatedAtAction(nameof(GetById), new { id = household.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, HouseholdUpdateDto dto)
    {
        var household = await _context.Households.FirstOrDefaultAsync(h => h.Id == id);
        if (household is null)
            return NotFound();

        if (!await _context.Apartments.AnyAsync(a => a.Id == dto.ApartmentId))
            return NotFound(new { message = "Apartment not found." });

        var householdCode = dto.HouseholdCode.Trim();
        if (await _context.Households.AnyAsync(h =>
                h.HouseholdCode == householdCode && h.Id != id))
        {
            return Conflict(new { message = "Household code already exists." });
        }

        household.HouseholdCode = householdCode;
        household.ApartmentId = dto.ApartmentId;
        household.IsActive = dto.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Household code already exists or the apartment reference is invalid." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var household = await _context.Households.FirstOrDefaultAsync(h => h.Id == id);
        if (household is null)
            return NotFound();

        if (await _context.Residents.AnyAsync(r => r.HouseholdId == id))
            return Conflict(new { message = "Cannot delete a household that has residents." });

        _context.Households.Remove(household);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Cannot delete the household because related data exists." });
        }

        return NoContent();
    }

    private IQueryable<HouseholdResponseDto> HouseholdResponses()
    {
        return _context.Households
            .AsNoTracking()
            .Select(h => new HouseholdResponseDto
            {
                Id = h.Id,
                HouseholdCode = h.HouseholdCode,
                ApartmentId = h.ApartmentId,
                ApartmentCode = h.Apartment == null ? string.Empty : h.Apartment.ApartmentCode,
                Building = h.Apartment == null ? string.Empty : h.Apartment.Building,
                CreatedAt = h.CreatedAt,
                IsActive = h.IsActive
            });
    }
}
