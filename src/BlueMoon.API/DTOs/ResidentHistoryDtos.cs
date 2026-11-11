using System.ComponentModel.DataAnnotations;

namespace BlueMoon.API.DTOs;

public sealed class ResidentHistoryCreateDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int ResidentId { get; set; }

    [Required]
    [RegularExpression("^(MoveIn|MoveOut|InformationChange)$",
        ErrorMessage = "HistoryType must be MoveIn, MoveOut, or InformationChange.")]
    public string HistoryType { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(200)]
    public string? Destination { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Note { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? PreviousValue { get; set; }

    [StringLength(2000)]
    public string? NewValue { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate == default)
        {
            yield return new ValidationResult(
                "StartDate is required.",
                new[] { nameof(StartDate) });
        }

        if (EndDate.HasValue && EndDate.Value < StartDate)
        {
            yield return new ValidationResult(
                "EndDate cannot be earlier than StartDate.",
                new[] { nameof(EndDate) });
        }

        if (string.Equals(HistoryType, "InformationChange", StringComparison.Ordinal) &&
            (string.IsNullOrWhiteSpace(PreviousValue) || string.IsNullOrWhiteSpace(NewValue)))
        {
            yield return new ValidationResult(
                "PreviousValue and NewValue are required for InformationChange.",
                new[] { nameof(PreviousValue), nameof(NewValue) });
        }
    }
}

public sealed class ResidentHistoryResponseDto
{
    public int Id { get; init; }
    public int ResidentId { get; init; }
    public string ResidentFullName { get; init; } = string.Empty;
    public int HouseholdId { get; init; }
    public string HouseholdCode { get; init; } = string.Empty;
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public string HistoryType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Destination { get; init; }
    public string Note { get; init; } = string.Empty;
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public int CreatedByUserId { get; init; }
}
