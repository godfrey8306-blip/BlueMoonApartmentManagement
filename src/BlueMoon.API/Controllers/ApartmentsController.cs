using BlueMoon.API.Data;
using BlueMoon.API.DTOs;
using BlueMoon.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueMoon.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApartmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ApartmentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/apartments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApartmentResponseDto>>> GetAll()
    {
        var apartments = await _context.Apartments
            .OrderBy(a => a.ApartmentCode)
            .Select(a => new ApartmentResponseDto
            {
                Id = a.Id,
                ApartmentCode = a.ApartmentCode,
                Building = a.Building,
                Floor = a.Floor,
                Area = a.Area,
                Status = a.Status
            })
            .ToListAsync();

        return Ok(apartments);
    }

    // GET: api/apartments/1
    [HttpGet("{id}")]
    public async Task<ActionResult<ApartmentResponseDto>> GetById(int id)
    {
        var apartment = await _context.Apartments.FindAsync(id);

        if (apartment == null)
            return NotFound();

        return Ok(new ApartmentResponseDto
        {
            Id = apartment.Id,
            ApartmentCode = apartment.ApartmentCode,
            Building = apartment.Building,
            Floor = apartment.Floor,
            Area = apartment.Area,
            Status = apartment.Status
        });
    }

    // POST: api/apartments
    [HttpPost]
    public async Task<ActionResult<ApartmentResponseDto>> Create(
        ApartmentCreateDto dto)
    {
        if (await _context.Apartments.AnyAsync(
            a => a.ApartmentCode == dto.ApartmentCode))
        {
            return Conflict("Mã căn hộ đã tồn tại.");
        }

        var apartment = new Apartment
        {
            ApartmentCode = dto.ApartmentCode,
            Building = dto.Building,
            Floor = dto.Floor,
            Area = dto.Area,
            Status = dto.Status
        };

        _context.Apartments.Add(apartment);
        await _context.SaveChangesAsync();

        var response = new ApartmentResponseDto
        {
            Id = apartment.Id,
            ApartmentCode = apartment.ApartmentCode,
            Building = apartment.Building,
            Floor = apartment.Floor,
            Area = apartment.Area,
            Status = apartment.Status
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = apartment.Id },
            response);
    }

    // PUT: api/apartments/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id, ApartmentUpdateDto dto)
    {
        var apartment = await _context.Apartments.FindAsync(id);

        if (apartment == null)
            return NotFound();

        if (await _context.Apartments.AnyAsync(
            a => a.ApartmentCode == dto.ApartmentCode && a.Id != id))
        {
            return Conflict("Mã căn hộ đã tồn tại.");
        }

        apartment.ApartmentCode = dto.ApartmentCode;
        apartment.Building = dto.Building;
        apartment.Floor = dto.Floor;
        apartment.Area = dto.Area;
        apartment.Status = dto.Status;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/apartments/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var apartment = await _context.Apartments.FindAsync(id);

        if (apartment == null)
            return NotFound();

        bool hasReferences =
            await _context.Households.AnyAsync(h => h.ApartmentId == id) ||
            await _context.Vehicles.AnyAsync(v => v.ApartmentId == id) ||
            await _context.FeeCharges.AnyAsync(f => f.ApartmentId == id) ||
            await _context.Payments.AnyAsync(p => p.ApartmentId == id);

        if (hasReferences)
            return Conflict("Không thể xóa căn hộ đang có dữ liệu liên quan.");

        _context.Apartments.Remove(apartment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}