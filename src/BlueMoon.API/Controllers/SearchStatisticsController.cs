using BlueMoon.API.Data;
using BlueMoon.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueMoon.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/search-statistics")]
public sealed class SearchStatisticsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SearchStatisticsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("households")]
    public async Task<ActionResult<IEnumerable<HouseholdSearchResponseDto>>> SearchHouseholds(
        [FromQuery] string? search,
        [FromQuery] int? apartmentId,
        [FromQuery] bool? isActive)
    {
        var query = _context.Households.AsNoTracking();

        if (apartmentId.HasValue)
            query = query.Where(h => h.ApartmentId == apartmentId.Value);
        if (isActive.HasValue)
            query = query.Where(h => h.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(h =>
                EF.Functions.ILike(h.HouseholdCode, $"%{term}%") ||
                (h.Apartment != null && EF.Functions.ILike(h.Apartment.ApartmentCode, $"%{term}%")) ||
                h.Residents.Any(r =>
                    EF.Functions.ILike(r.FullName, $"%{term}%") ||
                    EF.Functions.ILike(r.IdentityNumber, $"%{term}%") ||
                    EF.Functions.ILike(r.PhoneNumber, $"%{term}%")));
        }

        return Ok(await query
            .OrderBy(h => h.HouseholdCode)
            .Select(h => new HouseholdSearchResponseDto
            {
                Id = h.Id,
                HouseholdCode = h.HouseholdCode,
                ApartmentId = h.ApartmentId,
                ApartmentCode = h.Apartment == null ? string.Empty : h.Apartment.ApartmentCode,
                Building = h.Apartment == null ? string.Empty : h.Apartment.Building,
                IsActive = h.IsActive,
                ResidentCount = h.Residents.Count,
                Residents = h.Residents
                    .OrderBy(r => r.FullName)
                    .Select(r => new ResidentSearchResponseDto
                    {
                        Id = r.Id,
                        FullName = r.FullName,
                        IdentityNumber = r.IdentityNumber,
                        PhoneNumber = r.PhoneNumber,
                        Relationship = r.Relationship,
                        IsHouseholdHead = r.IsHouseholdHead,
                        Status = r.Status
                    })
                    .ToList()
            })
            .ToListAsync());
    }

    [HttpGet("charges")]
    public async Task<ActionResult<IEnumerable<FinancialSearchResponseDto>>> SearchCharges(
        [FromQuery] int? apartmentId,
        [FromQuery] int? feeCollectionId,
        [FromQuery] int? feeCategoryId,
        [FromQuery] string? status,
        [FromQuery] int? month,
        [FromQuery] int? year)
    {
        var query = FinancialCharges();
        query = ApplyChargeFilters(query, apartmentId, feeCollectionId, feeCategoryId, status, month, year);

        return Ok(await query
            .OrderBy(c => c.ApartmentCode)
            .ThenByDescending(c => c.FeeCollectionYear)
            .ThenByDescending(c => c.FeeCollectionMonth)
            .ThenBy(c => c.FeeCategoryName)
            .ToListAsync());
    }

    [HttpGet("payments")]
    public async Task<ActionResult<IEnumerable<PaymentSearchResponseDto>>> SearchPayments(
        [FromQuery] int? apartmentId,
        [FromQuery] int? feeCollectionId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? receiptNumber)
    {
        var query = _context.PaymentDetails
            .AsNoTracking()
            .Select(d => new PaymentSearchResponseDto
            {
                PaymentId = d.PaymentId,
                ApartmentId = d.Payment == null ? 0 : d.Payment.ApartmentId,
                ApartmentCode = d.Payment == null || d.Payment.Apartment == null
                    ? string.Empty
                    : d.Payment.Apartment.ApartmentCode,
                ReceiptNumber = d.Payment == null ? string.Empty : d.Payment.ReceiptNumber,
                PaymentDate = d.Payment == null ? default : d.Payment.PaymentDate,
                Amount = d.Amount,
                PaymentMethod = d.Payment == null ? string.Empty : d.Payment.PaymentMethod,
                FeeChargeId = d.FeeChargeId,
                FeeCategoryName = d.FeeCharge == null || d.FeeCharge.FeeCategory == null
                    ? string.Empty
                    : d.FeeCharge.FeeCategory.Name,
                FeeCollectionId = d.FeeCharge == null ? 0 : d.FeeCharge.FeeCollectionId,
                FeeCollectionName = d.FeeCharge == null || d.FeeCharge.FeeCollection == null
                    ? string.Empty
                    : d.FeeCharge.FeeCollection.Name
            });

        if (apartmentId.HasValue)
            query = query.Where(p => p.ApartmentId == apartmentId.Value);
        if (feeCollectionId.HasValue)
            query = query.Where(p => p.FeeCollectionId == feeCollectionId.Value);
        if (fromDate.HasValue)
            query = query.Where(p => p.PaymentDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(p => p.PaymentDate <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(receiptNumber))
        {
            var term = receiptNumber.Trim();
            query = query.Where(p => EF.Functions.ILike(p.ReceiptNumber, $"%{term}%"));
        }

        return Ok(await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.PaymentId)
            .ToListAsync());
    }

    [HttpGet("statistics/collections")]
    public async Task<ActionResult<IEnumerable<CollectionStatisticsDto>>> CollectionStatistics(
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] string? status)
    {
        var query = _context.FeeCollections.AsNoTracking();
        if (month.HasValue)
            query = query.Where(c => c.Month == month.Value);
        if (year.HasValue)
            query = query.Where(c => c.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status.Trim());

        var rows = await query
            .OrderByDescending(c => c.Year)
            .ThenByDescending(c => c.Month)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Month,
                c.Year,
                c.Status,
                ChargeCount = c.FeeCharges.Count,
                TotalAmount = c.FeeCharges.Sum(f => (decimal?)f.Amount) ?? 0m,
                PaidAmount = c.FeeCharges.Sum(f => (decimal?)f.PaidAmount) ?? 0m
            })
            .ToListAsync();

        return Ok(rows.Select(c => new CollectionStatisticsDto
        {
            FeeCollectionId = c.Id,
            FeeCollectionName = c.Name,
            Month = c.Month,
            Year = c.Year,
            Status = c.Status,
            ChargeCount = c.ChargeCount,
            TotalAmount = c.TotalAmount,
            PaidAmount = c.PaidAmount,
            OutstandingAmount = c.TotalAmount - c.PaidAmount,
            CollectionRate = CalculateRate(c.PaidAmount, c.TotalAmount)
        }));
    }

    [HttpGet("statistics/financial")]
    public async Task<ActionResult<FinancialStatisticsDto>> FinancialStatistics(
        [FromQuery] int? apartmentId,
        [FromQuery] int? feeCollectionId,
        [FromQuery] int? feeCategoryId)
    {
        var charges = _context.FeeCharges.AsNoTracking();
        if (apartmentId.HasValue)
            charges = charges.Where(c => c.ApartmentId == apartmentId.Value);
        if (feeCollectionId.HasValue)
            charges = charges.Where(c => c.FeeCollectionId == feeCollectionId.Value);
        if (feeCategoryId.HasValue)
            charges = charges.Where(c => c.FeeCategoryId == feeCategoryId.Value);

        var rows = await charges
            .Select(c => new { c.Amount, c.PaidAmount, c.Status })
            .ToListAsync();
        var payments = _context.PaymentDetails.AsNoTracking();
        if (apartmentId.HasValue)
            payments = payments.Where(d => d.Payment != null && d.Payment.ApartmentId == apartmentId.Value);
        if (feeCollectionId.HasValue)
            payments = payments.Where(d => d.FeeCharge != null && d.FeeCharge.FeeCollectionId == feeCollectionId.Value);
        if (feeCategoryId.HasValue)
            payments = payments.Where(d => d.FeeCharge != null && d.FeeCharge.FeeCategoryId == feeCategoryId.Value);

        return Ok(new FinancialStatisticsDto
        {
            ChargeCount = rows.Count,
            PaymentCount = await payments.Select(d => d.PaymentId).Distinct().CountAsync(),
            TotalAmount = rows.Sum(c => c.Amount),
            PaidAmount = rows.Sum(c => c.PaidAmount),
            OutstandingAmount = rows.Sum(c => c.Amount - c.PaidAmount),
            CollectionRate = CalculateRate(rows.Sum(c => c.PaidAmount), rows.Sum(c => c.Amount)),
            UnpaidChargeCount = rows.Count(c => c.Status == "Unpaid"),
            PartiallyPaidChargeCount = rows.Count(c => c.Status == "PartiallyPaid"),
            PaidChargeCount = rows.Count(c => c.Status == "Paid")
        });
    }

    [HttpGet("statistics/apartments")]
    public async Task<ActionResult<IEnumerable<ApartmentContributionStatisticsDto>>> ApartmentStatistics(
        [FromQuery] int? feeCollectionId,
        [FromQuery] int? apartmentId)
    {
        var query = _context.Apartments.AsNoTracking();
        if (apartmentId.HasValue)
            query = query.Where(a => a.Id == apartmentId.Value);

        var rows = await query
            .Select(a => new
            {
                a.Id,
                a.ApartmentCode
            })
            .ToListAsync();

        var charges = _context.FeeCharges.AsNoTracking();
        if (feeCollectionId.HasValue)
            charges = charges.Where(c => c.FeeCollectionId == feeCollectionId.Value);
        if (apartmentId.HasValue)
            charges = charges.Where(c => c.ApartmentId == apartmentId.Value);

        var financialRows = await charges
            .GroupBy(c => c.ApartmentId)
            .Select(g => new
            {
                ApartmentId = g.Key,
                ChargeCount = g.Count(),
                TotalAmount = g.Sum(c => c.Amount),
                PaidAmount = g.Sum(c => c.PaidAmount)
            })
            .ToListAsync();

        return Ok(rows.Select(a =>
        {
            var financial = financialRows.FirstOrDefault(f => f.ApartmentId == a.Id);
            var total = financial?.TotalAmount ?? 0m;
            var paid = financial?.PaidAmount ?? 0m;
            return new ApartmentContributionStatisticsDto
            {
                ApartmentId = a.Id,
                ApartmentCode = a.ApartmentCode,
                ChargeCount = financial?.ChargeCount ?? 0,
                TotalAmount = total,
                PaidAmount = paid,
                OutstandingAmount = total - paid,
                CollectionRate = CalculateRate(paid, total)
            };
        }));
    }

    private IQueryable<FinancialSearchResponseDto> FinancialCharges()
    {
        return _context.FeeCharges
            .AsNoTracking()
            .Select(c => new FinancialSearchResponseDto
            {
                FeeChargeId = c.Id,
                ApartmentId = c.ApartmentId,
                ApartmentCode = c.Apartment == null ? string.Empty : c.Apartment.ApartmentCode,
                FeeCollectionId = c.FeeCollectionId,
                FeeCollectionName = c.FeeCollection == null ? string.Empty : c.FeeCollection.Name,
                FeeCollectionMonth = c.FeeCollection == null ? 0 : c.FeeCollection.Month,
                FeeCollectionYear = c.FeeCollection == null ? 0 : c.FeeCollection.Year,
                FeeCategoryId = c.FeeCategoryId,
                FeeCategoryName = c.FeeCategory == null ? string.Empty : c.FeeCategory.Name,
                TotalAmount = c.Amount,
                PaidAmount = c.PaidAmount,
                OutstandingAmount = c.Amount - c.PaidAmount,
                Status = c.Status,
                DueDate = c.DueDate
            });
    }

    private static IQueryable<FinancialSearchResponseDto> ApplyChargeFilters(
        IQueryable<FinancialSearchResponseDto> query,
        int? apartmentId,
        int? feeCollectionId,
        int? feeCategoryId,
        string? status,
        int? month,
        int? year)
    {
        if (apartmentId.HasValue)
            query = query.Where(c => c.ApartmentId == apartmentId.Value);
        if (feeCollectionId.HasValue)
            query = query.Where(c => c.FeeCollectionId == feeCollectionId.Value);
        if (feeCategoryId.HasValue)
            query = query.Where(c => c.FeeCategoryId == feeCategoryId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status.Trim());
        if (month.HasValue)
            query = query.Where(c => c.FeeCollectionMonth == month.Value);
        if (year.HasValue)
            query = query.Where(c => c.FeeCollectionYear == year.Value);
        return query;
    }

    private static decimal CalculateRate(decimal paid, decimal total)
    {
        return total <= 0m ? 0m : decimal.Round(paid / total * 100m, 2, MidpointRounding.AwayFromZero);
    }
}
