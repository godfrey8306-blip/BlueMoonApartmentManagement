using System.ComponentModel.DataAnnotations;

namespace BlueMoon.API.DTOs;

public sealed class ResidentCreateDto
{
    [Range(1, int.MaxValue)]
    public int HouseholdId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string IdentityNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(30)]
    public string Gender { get; set; } = string.Empty;

    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string Relationship { get; set; } = string.Empty;

    public bool IsHouseholdHead { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Active";
}

public sealed class ResidentUpdateDto
{
    [Range(1, int.MaxValue)]
    public int HouseholdId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string IdentityNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(30)]
    public string Gender { get; set; } = string.Empty;

    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string Relationship { get; set; } = string.Empty;

    public bool IsHouseholdHead { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Active";
}

public sealed class ResidentResponseDto
{
    public int Id { get; init; }
    public int HouseholdId { get; init; }
    public string HouseholdCode { get; init; } = string.Empty;
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public string Building { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Relationship { get; init; } = string.Empty;
    public bool IsHouseholdHead { get; init; }
    public string Status { get; init; } = string.Empty;
}
