namespace BlueMoon.API.Models;

public class Vehicle
{
    public int Id { get; set; }
    public int ApartmentId { get; set; }
    public int? ResidentId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Apartment? Apartment { get; set; }
    public Resident? Resident { get; set; }
}