using System.ComponentModel.DataAnnotations;

namespace BlueMoon.API.DTOs;

public class ApartmentCreateDto
{
    [Required]
    public string ApartmentCode { get; set; } = string.Empty;

    [Required]
    public string Building { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Floor { get; set; }

    [Range(0.1, 10000)]
    public decimal Area { get; set; }

    public string Status { get; set; } = "Available";
}