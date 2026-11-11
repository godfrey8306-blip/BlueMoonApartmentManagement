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
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentResponseDto>>> GetAll(
        [FromQuery] int? apartmentId,
        [FromQuery] int? feeChargeId,
        [FromQuery] string? receiptNumber)
    {
        var query = PaymentResponses();

        if (apartmentId.HasValue)
            query = query.Where(p => p.ApartmentId == apartmentId.Value);
        if (feeChargeId.HasValue)
            query = query.Where(p => p.Details.Any(d => d.FeeChargeId == feeChargeId.Value));
        if (!string.IsNullOrWhiteSpace(receiptNumber))
        {
            var search = receiptNumber.Trim();
            query = query.Where(p => EF.Functions.ILike(p.ReceiptNumber, $"%{search}%"));
        }

        return Ok(await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentResponseDto>> GetById(int id)
    {
        var payment = await PaymentResponses()
            .FirstOrDefaultAsync(p => p.Id == id);

        return payment is null ? NotFound() : Ok(payment);
    }

    [HttpGet("apartment/{apartmentId:int}")]
    public async Task<ActionResult<IEnumerable<PaymentResponseDto>>> GetByApartment(int apartmentId)
    {
        if (!await _context.Apartments.AnyAsync(a => a.Id == apartmentId))
            return NotFound(new { message = "Apartment not found." });

        return Ok(await PaymentResponses()
            .Where(p => p.ApartmentId == apartmentId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync());
    }

    [HttpGet("fee-charge/{feeChargeId:int}")]
    public async Task<ActionResult<IEnumerable<PaymentDetailHistoryDto>>> GetByFeeCharge(int feeChargeId)
    {
        if (!await _context.FeeCharges.AnyAsync(c => c.Id == feeChargeId))
            return NotFound(new { message = "Fee charge not found." });

        var history = await _context.PaymentDetails
            .AsNoTracking()
            .Where(d => d.FeeChargeId == feeChargeId)
            .OrderByDescending(d => d.Payment!.PaymentDate)
            .ThenByDescending(d => d.Id)
            .Select(d => new PaymentDetailHistoryDto
            {
                PaymentId = d.PaymentId,
                ReceiptNumber = d.Payment == null ? string.Empty : d.Payment.ReceiptNumber,
                PaymentDate = d.Payment == null ? default : d.Payment.PaymentDate,
                PaymentMethod = d.Payment == null ? string.Empty : d.Payment.PaymentMethod,
                Amount = d.Amount
            })
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponseDto>> Create(PaymentCreateDto dto)
    {
        var receivedByUserId = GetAuthenticatedUserId();
        if (!receivedByUserId.HasValue)
            return Unauthorized(new { message = "Authenticated user id is missing from the token." });

        if (!await _context.Users.AnyAsync(u =>
                u.Id == receivedByUserId.Value && u.IsActive && u.Role == "Admin"))
            return Forbid();

        var receiptNumber = dto.ReceiptNumber.Trim();
        if (await _context.Payments.AnyAsync(p => p.ReceiptNumber == receiptNumber))
            return Conflict(new { message = "Receipt number already exists." });

        if (!await _context.Apartments.AnyAsync(a => a.Id == dto.ApartmentId))
            return NotFound(new { message = "Apartment not found." });

        if (dto.ResidentId.HasValue)
        {
            var residentBelongsToApartment = await _context.Residents
                .AnyAsync(r => r.Id == dto.ResidentId.Value &&
                    r.Household != null && r.Household.ApartmentId == dto.ApartmentId);
            if (!residentBelongsToApartment)
                return BadRequest(new { message = "Resident does not belong to the selected apartment." });
        }

        var detailIds = dto.Details.Select(d => d.FeeChargeId).ToList();
        if (detailIds.Count != detailIds.Distinct().Count())
            return BadRequest(new { message = "A fee charge may appear only once in a payment." });

        var charges = await _context.FeeCharges
            .Where(c => detailIds.Contains(c.Id))
            .ToListAsync();
        if (charges.Count != detailIds.Count)
            return NotFound(new { message = "One or more fee charges were not found." });

        if (charges.Any(c => c.ApartmentId != dto.ApartmentId))
            return BadRequest(new { message = "All fee charges must belong to the selected apartment." });

        var requestedAmounts = dto.Details.ToDictionary(d => d.FeeChargeId, d => RoundMoney(d.Amount));
        foreach (var charge in charges)
        {
            var outstanding = RoundMoney(charge.Amount - charge.PaidAmount);
            var paymentAmount = requestedAmounts[charge.Id];
            if (paymentAmount > outstanding)
                return Conflict(new { message = $"Payment amount for fee charge {charge.Id} exceeds its outstanding amount." });
        }

        var totalAmount = RoundMoney(requestedAmounts.Values.Sum());
        if (totalAmount <= 0m)
            return BadRequest(new { message = "Payment total must be greater than zero." });

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = new Payment
            {
                ApartmentId = dto.ApartmentId,
                ResidentId = dto.ResidentId,
                ReceivedByUserId = receivedByUserId.Value,
                ReceiptNumber = receiptNumber,
                PaymentDate = dto.PaymentDate ?? DateTime.UtcNow,
                TotalAmount = totalAmount,
                PaymentMethod = dto.PaymentMethod.Trim(),
                Note = Normalize(dto.Note)
            };

            foreach (var charge in charges)
            {
                var amount = requestedAmounts[charge.Id];
                payment.PaymentDetails.Add(new PaymentDetail
                {
                    FeeCharge = charge,
                    Amount = amount
                });
                charge.PaidAmount = RoundMoney(charge.PaidAmount + amount);
                ApplyPaymentState(charge);
            }

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = await PaymentResponses().FirstAsync(p => p.Id == payment.Id);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, response);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "The payment could not be saved." });
        }
    }

    private IQueryable<PaymentResponseDto> PaymentResponses()
    {
        return _context.Payments
            .AsNoTracking()
            .Select(p => new PaymentResponseDto
            {
                Id = p.Id,
                ApartmentId = p.ApartmentId,
                ApartmentCode = p.Apartment == null ? string.Empty : p.Apartment.ApartmentCode,
                ResidentId = p.ResidentId,
                ResidentFullName = p.Resident == null ? string.Empty : p.Resident.FullName,
                ReceivedByUserId = p.ReceivedByUserId,
                ReceiptNumber = p.ReceiptNumber,
                PaymentDate = p.PaymentDate,
                TotalAmount = p.TotalAmount,
                PaymentMethod = p.PaymentMethod,
                Note = p.Note ?? string.Empty,
                Details = p.PaymentDetails
                    .OrderBy(d => d.Id)
                    .Select(d => new PaymentDetailResponseDto
                    {
                        Id = d.Id,
                        FeeChargeId = d.FeeChargeId,
                        FeeCategoryName = d.FeeCharge == null || d.FeeCharge.FeeCategory == null
                            ? string.Empty
                            : d.FeeCharge.FeeCategory.Name,
                        FeeCollectionId = d.FeeCharge == null ? 0 : d.FeeCharge.FeeCollectionId,
                        FeeCollectionName = d.FeeCharge == null || d.FeeCharge.FeeCollection == null
                            ? string.Empty
                            : d.FeeCharge.FeeCollection.Name,
                        Amount = d.Amount
                    })
                    .ToList()
            });
    }

    private int? GetAuthenticatedUserId()
    {
        var subject = User.FindFirstValue("sub") ??
                      User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, out var userId) ? userId : null;
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

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class PaymentDetailHistoryDto
{
    public int PaymentId { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
