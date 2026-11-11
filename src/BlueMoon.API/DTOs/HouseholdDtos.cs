using System.ComponentModel.DataAnnotations;

namespace BlueMoon.API.DTOs;

public sealed class HouseholdCreateDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string HouseholdCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ApartmentId { get; set; }
}

public sealed class HouseholdUpdateDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string HouseholdCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ApartmentId { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class HouseholdResponseDto
{
    public int Id { get; init; }
    public string HouseholdCode { get; init; } = string.Empty;
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public string Building { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
}
