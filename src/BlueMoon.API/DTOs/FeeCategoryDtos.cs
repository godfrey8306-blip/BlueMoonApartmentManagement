using System.ComponentModel.DataAnnotations;

namespace BlueMoon.API.DTOs;

public sealed class FeeCategoryCreateDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Unit { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal UnitPrice { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string CalculationMethod { get; set; } = string.Empty;

    public bool IsMandatory { get; set; }
}

public sealed class FeeCategoryUpdateDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Unit { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal UnitPrice { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string CalculationMethod { get; set; } = string.Empty;

    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class FeeCategoryResponseDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public string CalculationMethod { get; init; } = string.Empty;
    public bool IsMandatory { get; init; }
    public bool IsActive { get; init; }
}
