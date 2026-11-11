using BlueMoon.API.Data;
using BlueMoon.API.DTOs;
using BlueMoon.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueMoon.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ResidentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ResidentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResidentResponseDto>>> GetAll(
        [FromQuery] int? householdId,
        [FromQuery] int? apartmentId,
        [FromQuery] string? name,
        [FromQuery] string? status)
    {
        var query = ResidentResponses();

        if (householdId.HasValue)
            query = query.Where(r => r.HouseholdId == householdId.Value);

        if (apartmentId.HasValue)
            query = query.Where(r => r.ApartmentId == apartmentId.Value);

        if (!string.IsNullOrWhiteSpace(name))
        {
            var search = name.Trim();
            query = query.Where(r => EF.Functions.ILike(r.FullName, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(r => r.Status == normalizedStatus);
        }

        return Ok(await query
            .OrderBy(r => r.FullName)
            .ThenBy(r => r.Id)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResidentResponseDto>> GetById(int id)
    {
        var resident = await ResidentResponses()
            .FirstOrDefaultAsync(r => r.Id == id);

        return resident is null ? NotFound() : Ok(resident);
    }

    [HttpPost]
    public async Task<ActionResult<ResidentResponseDto>> Create(ResidentCreateDto dto)
    {
        var validationError = ValidateDateOfBirth(dto.DateOfBirth);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        if (!await _context.Households.AnyAsync(h => h.Id == dto.HouseholdId))
            return NotFound(new { message = "Household not found." });

        var identityNumber = dto.IdentityNumber.Trim();
        if (await _context.Residents.AnyAsync(r => r.IdentityNumber == identityNumber))
            return Conflict(new { message = "Identity number already exists." });

        var resident = new Resident
        {
            HouseholdId = dto.HouseholdId,
            FullName = dto.FullName.Trim(),
            IdentityNumber = identityNumber,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Relationship = dto.Relationship.Trim(),
            IsHouseholdHead = dto.IsHouseholdHead,
            Status = dto.Status.Trim()
        };

        _context.Residents.Add(resident);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Identity number already exists or the household reference is invalid." });
        }

        var response = await ResidentResponses()
            .FirstAsync(r => r.Id == resident.Id);

        return CreatedAtAction(nameof(GetById), new { id = resident.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ResidentUpdateDto dto)
    {
        var validationError = ValidateDateOfBirth(dto.DateOfBirth);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var resident = await _context.Residents.FirstOrDefaultAsync(r => r.Id == id);
        if (resident is null)
            return NotFound();

        if (!await _context.Households.AnyAsync(h => h.Id == dto.HouseholdId))
            return NotFound(new { message = "Household not found." });

        var identityNumber = dto.IdentityNumber.Trim();
        if (await _context.Residents.AnyAsync(r =>
                r.IdentityNumber == identityNumber && r.Id != id))
        {
            return Conflict(new { message = "Identity number already exists." });
        }

        resident.HouseholdId = dto.HouseholdId;
        resident.FullName = dto.FullName.Trim();
        resident.IdentityNumber = identityNumber;
        resident.DateOfBirth = dto.DateOfBirth;
        resident.Gender = dto.Gender.Trim();
        resident.PhoneNumber = dto.PhoneNumber.Trim();
        resident.Relationship = dto.Relationship.Trim();
        resident.IsHouseholdHead = dto.IsHouseholdHead;
        resident.Status = dto.Status.Trim();

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Identity number already exists or the household reference is invalid." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resident = await _context.Residents.FirstOrDefaultAsync(r => r.Id == id);
        if (resident is null)
            return NotFound();

        var hasRelatedData =
            await _context.ResidenceHistories.AnyAsync(h => h.ResidentId == id) ||
            await _context.Vehicles.AnyAsync(v => v.ResidentId == id) ||
            await _context.Payments.AnyAsync(p => p.ResidentId == id);

        if (hasRelatedData)
            return Conflict(new { message = "Cannot delete a resident with related data." });

        _context.Residents.Remove(resident);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Cannot delete the resident because related data exists." });
        }

        return NoContent();
    }

    private IQueryable<ResidentResponseDto> ResidentResponses()
    {
        return _context.Residents
            .AsNoTracking()
            .Select(r => new ResidentResponseDto
            {
                Id = r.Id,
                HouseholdId = r.HouseholdId,
                HouseholdCode = r.Household == null ? string.Empty : r.Household.HouseholdCode,
                ApartmentId = r.Household == null ? 0 : r.Household.ApartmentId,
                ApartmentCode = r.Household == null || r.Household.Apartment == null
                    ? string.Empty
                    : r.Household.Apartment.ApartmentCode,
                Building = r.Household == null || r.Household.Apartment == null
                    ? string.Empty
                    : r.Household.Apartment.Building,
                FullName = r.FullName,
                DateOfBirth = r.DateOfBirth,
                Gender = r.Gender,
                PhoneNumber = r.PhoneNumber,
                Relationship = r.Relationship,
                IsHouseholdHead = r.IsHouseholdHead,
                Status = r.Status
            });
    }

    private static string? ValidateDateOfBirth(DateTime? dateOfBirth)
    {
        return dateOfBirth.HasValue && dateOfBirth.Value.Date > DateTime.UtcNow.Date
            ? "Date of birth cannot be in the future."
            : null;
    }
}
