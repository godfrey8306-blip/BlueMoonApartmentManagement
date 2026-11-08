namespace BlueMoon.API.Models;

public class Apartment
{
    public int Id { get; set; }
    public string ApartmentCode { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public int Floor { get; set; }
    public decimal Area { get; set; }
    public string Status { get; set; } = "Available";

    public ICollection<Household> Households { get; set; } = new List<Household>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}