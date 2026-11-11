using BlueMoon.API.Data;
using BlueMoon.API.DTOs;
using BlueMoon.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueMoon.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/fee-charges")]
public sealed class FeeChargesController : ControllerBase
{
    private static readonly string[] PaymentStatuses = ["Unpaid", "PartiallyPaid", "Paid"];
    private readonly AppDbContext _context;

    public FeeChargesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FeeChargeResponseDto>>> GetAll(
        [FromQuery] int? feeCollectionId,
        [FromQuery] int? apartmentId,
        [FromQuery] int? feeCategoryId,
        [FromQuery] string? status)
    {
        var query = FeeChargeResponses();

        if (feeCollectionId.HasValue)
            query = query.Where(c => c.FeeCollectionId == feeCollectionId.Value);
        if (apartmentId.HasValue)
            query = query.Where(c => c.ApartmentId == apartmentId.Value);
        if (feeCategoryId.HasValue)
            query = query.Where(c => c.FeeCategoryId == feeCategoryId.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            if (!PaymentStatuses.Contains(normalizedStatus, StringComparer.Ordinal))
                return BadRequest(new { message = "Status must be Unpaid, PartiallyPaid, or Paid." });
            query = query.Where(c => c.Status == normalizedStatus);
        }

        return Ok(await query
            .OrderBy(c => c.ApartmentCode)
            .ThenBy(c => c.FeeCollectionYear)
            .ThenBy(c => c.FeeCollectionMonth)
            .ThenBy(c => c.FeeCategoryName)
            .ThenBy(c => c.Id)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FeeChargeResponseDto>> GetById(int id)
    {
        var charge = await FeeChargeResponses()
            .FirstOrDefaultAsync(c => c.Id == id);

        return charge is null ? NotFound() : Ok(charge);
    }

    [HttpGet("apartment/{apartmentId:int}")]
    public async Task<ActionResult<FeeChargeDebtSummaryDto>> GetByApartment(
        int apartmentId,
        [FromQuery] int? feeCollectionId,
        [FromQuery] string? status)
    {
        if (!await _context.Apartments.AnyAsync(a => a.Id == apartmentId))
            return NotFound(new { message = "Apartment not found." });

        var charges = await GetFilteredCharges(feeCollectionId, apartmentId, status);
        return Ok(await CreateSummary(apartmentId, feeCollectionId, charges));
    }

    [HttpGet("collection/{collectionId:int}")]
    public async Task<ActionResult<IEnumerable<FeeChargeResponseDto>>> GetByCollection(
        int collectionId,
        [FromQuery] int? apartmentId,
        [FromQuery] string? status)
    {
        if (!await _context.FeeCollections.AnyAsync(c => c.Id == collectionId))
            return NotFound(new { message = "Fee collection not found." });

        var charges = await GetFilteredCharges(collectionId, apartmentId, status);
        return Ok(charges);
    }

    [HttpGet("debt-summary")]
    public async Task<ActionResult<FeeChargeDebtSummaryDto>> GetDebtSummary(
        [FromQuery] int apartmentId,
        [FromQuery] int? feeCollectionId,
        [FromQuery] string? status)
    {
        if (!await _context.Apartments.AnyAsync(a => a.Id == apartmentId))
            return NotFound(new { message = "Apartment not found." });

        var charges = await GetFilteredCharges(feeCollectionId, apartmentId, status);
        return Ok(await CreateSummary(apartmentId, feeCollectionId, charges));
    }

    [HttpPost]
    public async Task<ActionResult<FeeChargeResponseDto>> Create(FeeChargeCreateDto dto)
    {
        var collection = await _context.FeeCollections
            .FirstOrDefaultAsync(c => c.Id == dto.FeeCollectionId);
        if (collection is null)
            return NotFound(new { message = "Fee collection not found." });
        if (!string.Equals(collection.Status, "Open", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Fee charges can only be created in an open fee collection." });

        var apartmentExists = await _context.Apartments.AnyAsync(a => a.Id == dto.ApartmentId);
        if (!apartmentExists)
            return NotFound(new { message = "Apartment not found." });

        var category = await _context.FeeCategories
            .FirstOrDefaultAsync(c => c.Id == dto.FeeCategoryId);
        if (category is null)
            return NotFound(new { message = "Fee category not found." });
        if (!category.IsActive)
            return Conflict(new { message = "The fee category is inactive." });

        if (await HasDuplicate(dto.FeeCollectionId, dto.ApartmentId, dto.FeeCategoryId))
            return Conflict(new { message = "A fee charge already exists for this apartment, collection, and category." });

        var unitPrice = RoundMoney(category.UnitPrice);
        var amount = CalculateAmount(dto.Quantity, unitPrice);
        var charge = new FeeCharge
        {
            FeeCollectionId = dto.FeeCollectionId,
            ApartmentId = dto.ApartmentId,
            FeeCategoryId = dto.FeeCategoryId,
            Quantity = dto.Quantity,
            UnitPrice = unitPrice,
            Amount = amount,
            PaidAmount = 0m,
            DueDate = dto.DueDate,
            Status = "Unpaid"
        };

        _context.FeeCharges.Add(charge);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The fee charge could not be saved." });
        }

        var response = await FeeChargeResponses().FirstAsync(c => c.Id == charge.Id);
        return CreatedAtAction(nameof(GetById), new { id = charge.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, FeeChargeUpdateDto dto)
    {
        var charge = await _context.FeeCharges.FirstOrDefaultAsync(c => c.Id == id);
        if (charge is null)
            return NotFound();

        var amount = CalculateAmount(dto.Quantity, charge.UnitPrice);
        if (amount < charge.PaidAmount)
            return Conflict(new { message = "Total amount cannot be less than the amount already paid." });

        charge.Quantity = dto.Quantity;
        charge.Amount = amount;
        charge.DueDate = dto.DueDate;
        ApplyPaymentState(charge);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The fee charge could not be updated." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var charge = await _context.FeeCharges.FirstOrDefaultAsync(c => c.Id == id);
        if (charge is null)
            return NotFound();

        if (charge.PaidAmount > 0m || await _context.PaymentDetails.AnyAsync(d => d.FeeChargeId == id))
            return Conflict(new { message = "Cannot delete a fee charge with payment history." });

        _context.FeeCharges.Remove(charge);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Cannot delete the fee charge because related data exists." });
        }

        return NoContent();
    }

    private IQueryable<FeeChargeResponseDto> FeeChargeResponses()
    {
        return _context.FeeCharges
            .AsNoTracking()
            .Select(c => new FeeChargeResponseDto
            {
                Id = c.Id,
                FeeCollectionId = c.FeeCollectionId,
                FeeCollectionName = c.FeeCollection == null ? string.Empty : c.FeeCollection.Name,
                FeeCollectionMonth = c.FeeCollection == null ? 0 : c.FeeCollection.Month,
                FeeCollectionYear = c.FeeCollection == null ? 0 : c.FeeCollection.Year,
                FeeCollectionStatus = c.FeeCollection == null ? string.Empty : c.FeeCollection.Status,
                ApartmentId = c.ApartmentId,
                ApartmentCode = c.Apartment == null ? string.Empty : c.Apartment.ApartmentCode,
                ApartmentBuilding = c.Apartment == null ? string.Empty : c.Apartment.Building,
                FeeCategoryId = c.FeeCategoryId,
                FeeCategoryName = c.FeeCategory == null ? string.Empty : c.FeeCategory.Name,
                FeeCategoryUnit = c.FeeCategory == null ? string.Empty : c.FeeCategory.Unit,
                CalculationMethod = c.FeeCategory == null ? string.Empty : c.FeeCategory.CalculationMethod,
                Quantity = c.Quantity,
                UnitPrice = c.UnitPrice,
                TotalAmount = c.Amount,
                PaidAmount = c.PaidAmount,
                OutstandingAmount = c.Amount - c.PaidAmount,
                DueDate = c.DueDate,
                Status = c.Status
            });
    }

    private async Task<List<FeeChargeResponseDto>> GetFilteredCharges(
        int? feeCollectionId,
        int? apartmentId,
        string? status)
    {
        var query = FeeChargeResponses();
        if (feeCollectionId.HasValue)
            query = query.Where(c => c.FeeCollectionId == feeCollectionId.Value);
        if (apartmentId.HasValue)
            query = query.Where(c => c.ApartmentId == apartmentId.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            if (!PaymentStatuses.Contains(normalizedStatus, StringComparer.Ordinal))
                throw new BadHttpRequestException("Status must be Unpaid, PartiallyPaid, or Paid.");
            query = query.Where(c => c.Status == normalizedStatus);
        }

        return await query.OrderBy(c => c.Id).ToListAsync();
    }

    private async Task<FeeChargeDebtSummaryDto> CreateSummary(
        int apartmentId,
        int? feeCollectionId,
        List<FeeChargeResponseDto> charges)
    {
        var apartmentCode = await _context.Apartments
            .Where(a => a.Id == apartmentId)
            .Select(a => a.ApartmentCode)
            .FirstAsync();

        return new FeeChargeDebtSummaryDto
        {
            ApartmentId = apartmentId,
            ApartmentCode = apartmentCode,
            FeeCollectionId = feeCollectionId,
            TotalAmount = charges.Sum(c => c.TotalAmount),
            PaidAmount = charges.Sum(c => c.PaidAmount),
            OutstandingAmount = charges.Sum(c => c.OutstandingAmount),
            ChargeCount = charges.Count,
            Charges = charges
        };
    }

    private Task<bool> HasDuplicate(int collectionId, int apartmentId, int categoryId)
    {
        return _context.FeeCharges.AnyAsync(c =>
            c.FeeCollectionId == collectionId &&
            c.ApartmentId == apartmentId &&
            c.FeeCategoryId == categoryId);
    }

    private static decimal CalculateAmount(decimal quantity, decimal unitPrice)
    {
        return RoundMoney(quantity * unitPrice);
    }

    private static decimal RoundMoney(decimal amount)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private static void ApplyPaymentState(FeeCharge charge)
    {
        charge.Status = charge.PaidAmount switch
        {
            <= 0m => "Unpaid",
            _ when charge.PaidAmount < charge.Amount => "PartiallyPaid",
            _ => "Paid"
        };
    }
}
