using System.ComponentModel.DataAnnotations;

namespace BlueMoon.API.DTOs;

public sealed class FeeChargeCreateDto
{
    [Range(1, int.MaxValue)]
    public int FeeCollectionId { get; set; }

    [Range(1, int.MaxValue)]
    public int ApartmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int FeeCategoryId { get; set; }

    [Range(typeof(decimal), "0.01", "1000000000000")]
    public decimal Quantity { get; set; }

    public DateTime? DueDate { get; set; }
}

public sealed class FeeChargeUpdateDto
{
    [Range(typeof(decimal), "0.01", "1000000000000")]
    public decimal Quantity { get; set; }

    public DateTime? DueDate { get; set; }
}

public sealed class FeeChargeResponseDto
{
    public int Id { get; init; }
    public int FeeCollectionId { get; init; }
    public string FeeCollectionName { get; init; } = string.Empty;
    public int FeeCollectionMonth { get; init; }
    public int FeeCollectionYear { get; init; }
    public string FeeCollectionStatus { get; init; } = string.Empty;
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public string ApartmentBuilding { get; init; } = string.Empty;
    public int FeeCategoryId { get; init; }
    public string FeeCategoryName { get; init; } = string.Empty;
    public string FeeCategoryUnit { get; init; } = string.Empty;
    public string CalculationMethod { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount { get; init; }
    public DateTime? DueDate { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class FeeChargeDebtSummaryDto
{
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public int? FeeCollectionId { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount { get; init; }
    public int ChargeCount { get; init; }
    public IReadOnlyCollection<FeeChargeResponseDto> Charges { get; init; } = [];
}
