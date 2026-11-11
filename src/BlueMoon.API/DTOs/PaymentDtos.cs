using System.ComponentModel.DataAnnotations;

namespace BlueMoon.API.DTOs;

public sealed class PaymentCreateDto
{
    [Range(1, int.MaxValue)]
    public int ApartmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ResidentId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ReceiptNumber { get; set; } = string.Empty;

    public DateTime? PaymentDate { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }

    [Required]
    [MinLength(1)]
    public List<PaymentDetailCreateDto> Details { get; set; } = [];
}

public sealed class PaymentDetailCreateDto
{
    [Range(1, int.MaxValue)]
    public int FeeChargeId { get; set; }

    [Range(typeof(decimal), "0.01", "1000000000000")]
    public decimal Amount { get; set; }
}

public sealed class PaymentResponseDto
{
    public int Id { get; init; }
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public int? ResidentId { get; init; }
    public string ResidentFullName { get; init; } = string.Empty;
    public int ReceivedByUserId { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public IReadOnlyCollection<PaymentDetailResponseDto> Details { get; init; } = [];
}

public sealed class PaymentDetailResponseDto
{
    public int Id { get; init; }
    public int FeeChargeId { get; init; }
    public string FeeCategoryName { get; init; } = string.Empty;
    public int FeeCollectionId { get; init; }
    public string FeeCollectionName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
