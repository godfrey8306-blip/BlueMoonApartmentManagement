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
[Route("api/residence-histories")]
public sealed class ResidentHistoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ResidentHistoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResidentHistoryResponseDto>>> GetAll(
        [FromQuery] int? residentId,
        [FromQuery] string? historyType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var query = HistoryResponses();

        if (residentId.HasValue)
            query = query.Where(h => h.ResidentId == residentId.Value);

        if (!string.IsNullOrWhiteSpace(historyType))
        {
            var normalizedType = historyType.Trim();
            query = query.Where(h => h.HistoryType == normalizedType);
        }

        if (fromDate.HasValue)
            query = query.Where(h => h.StartDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(h => h.StartDate <= toDate.Value);

        return Ok(await query
            .OrderByDescending(h => h.StartDate)
            .ThenByDescending(h => h.Id)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResidentHistoryResponseDto>> GetById(int id)
    {
        var history = await HistoryResponses()
            .FirstOrDefaultAsync(h => h.Id == id);

        return history is null ? NotFound() : Ok(history);
    }

    [HttpGet("resident/{residentId:int}")]
    public async Task<ActionResult<IEnumerable<ResidentHistoryResponseDto>>> GetByResident(int residentId)
    {
        if (!await _context.Residents.AnyAsync(r => r.Id == residentId))
            return NotFound(new { message = "Resident not found." });

        var history = await HistoryResponses()
            .Where(h => h.ResidentId == residentId)
            .OrderByDescending(h => h.StartDate)
            .ThenByDescending(h => h.Id)
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost]
    public async Task<ActionResult<ResidentHistoryResponseDto>> Create(ResidentHistoryCreateDto dto)
    {
        var creatorId = GetAuthenticatedUserId();
        if (!creatorId.HasValue)
            return Unauthorized(new { message = "Authenticated user id is missing from the token." });

        var creatorIsActiveAdmin = await _context.Users.AnyAsync(u =>
            u.Id == creatorId.Value && u.IsActive && u.Role == "Admin");
        if (!creatorIsActiveAdmin)
            return Forbid();

        if (!await _context.Residents.AnyAsync(r => r.Id == dto.ResidentId))
            return NotFound(new { message = "Resident not found." });

        var history = new ResidenceHistory
        {
            ResidentId = dto.ResidentId,
            HistoryType = dto.HistoryType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Destination = Normalize(dto.Destination),
            Note = dto.Note.Trim(),
            PreviousValue = Normalize(dto.PreviousValue),
            NewValue = Normalize(dto.NewValue),
            CreatedByUserId = creatorId.Value
        };

        _context.ResidenceHistories.Add(history);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The resident history could not be saved." });
        }

        var response = await HistoryResponses()
            .FirstAsync(h => h.Id == history.Id);

        return CreatedAtAction(nameof(GetById), new { id = history.Id }, response);
    }

    private IQueryable<ResidentHistoryResponseDto> HistoryResponses()
    {
        return _context.ResidenceHistories
            .AsNoTracking()
            .Select(h => new ResidentHistoryResponseDto
            {
                Id = h.Id,
                ResidentId = h.ResidentId,
                ResidentFullName = h.Resident == null ? string.Empty : h.Resident.FullName,
                HouseholdId = h.Resident == null ? 0 : h.Resident.HouseholdId,
                HouseholdCode = h.Resident == null || h.Resident.Household == null
                    ? string.Empty
                    : h.Resident.Household.HouseholdCode,
                ApartmentId = h.Resident == null || h.Resident.Household == null
                    ? 0
                    : h.Resident.Household.ApartmentId,
                ApartmentCode = h.Resident == null || h.Resident.Household == null || h.Resident.Household.Apartment == null
                    ? string.Empty
                    : h.Resident.Household.Apartment.ApartmentCode,
                HistoryType = h.HistoryType,
                StartDate = h.StartDate,
                EndDate = h.EndDate,
                Destination = h.Destination,
                Note = h.Note ?? string.Empty,
                PreviousValue = h.PreviousValue,
                NewValue = h.NewValue,
                CreatedByUserId = h.CreatedByUserId ?? 0
            });
    }

    private int? GetAuthenticatedUserId()
    {
        var subject = User.FindFirstValue("sub") ??
                      User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(subject, out var userId) ? userId : null;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
