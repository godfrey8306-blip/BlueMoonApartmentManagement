namespace BlueMoon.API.DTOs;

public class ApartmentResponseDto
{
    public int Id { get; set; }
    public string ApartmentCode { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public int Floor { get; set; }
    public decimal Area { get; set; }
    public string Status { get; set; } = string.Empty;
}