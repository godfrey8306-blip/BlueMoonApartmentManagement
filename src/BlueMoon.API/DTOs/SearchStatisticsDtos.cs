namespace BlueMoon.API.DTOs;

public sealed class HouseholdSearchResponseDto
{
    public int Id { get; init; }
    public string HouseholdCode { get; init; } = string.Empty;
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public string Building { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int ResidentCount { get; init; }
    public IReadOnlyCollection<ResidentSearchResponseDto> Residents { get; init; } = [];
}

public sealed class ResidentSearchResponseDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string IdentityNumber { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Relationship { get; init; } = string.Empty;
    public bool IsHouseholdHead { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class FinancialSearchResponseDto
{
    public int FeeChargeId { get; init; }
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public int FeeCollectionId { get; init; }
    public string FeeCollectionName { get; init; } = string.Empty;
    public int FeeCollectionMonth { get; init; }
    public int FeeCollectionYear { get; init; }
    public int FeeCategoryId { get; init; }
    public string FeeCategoryName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
}

public sealed class PaymentSearchResponseDto
{
    public int PaymentId { get; init; }
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public string ReceiptNumber { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public int FeeChargeId { get; init; }
    public string FeeCategoryName { get; init; } = string.Empty;
    public int FeeCollectionId { get; init; }
    public string FeeCollectionName { get; init; } = string.Empty;
}

public sealed class CollectionStatisticsDto
{
    public int FeeCollectionId { get; init; }
    public string FeeCollectionName { get; init; } = string.Empty;
    public int Month { get; init; }
    public int Year { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ChargeCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount { get; init; }
    public decimal CollectionRate { get; init; }
}

public sealed class FinancialStatisticsDto
{
    public int ChargeCount { get; init; }
    public int PaymentCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount { get; init; }
    public decimal CollectionRate { get; init; }
    public int UnpaidChargeCount { get; init; }
    public int PartiallyPaidChargeCount { get; init; }
    public int PaidChargeCount { get; init; }
}

public sealed class ApartmentContributionStatisticsDto
{
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public int ChargeCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount { get; init; }
    public decimal CollectionRate { get; init; }
}
