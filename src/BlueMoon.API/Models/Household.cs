namespace BlueMoon.API.Models;

public class Household
{
    public int Id { get; set; }
    public string HouseholdCode { get; set; } = string.Empty;
    public int ApartmentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Apartment? Apartment { get; set; }
    public ICollection<Resident> Residents { get; set; } = new List<Resident>();
}