namespace BlueMoon.API.Models;

public class Resident
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public bool IsHouseholdHead { get; set; }
    public string Status { get; set; } = "Active";

    public Household? Household { get; set; }
    public ICollection<ResidenceHistory> ResidenceHistories { get; set; } = new List<ResidenceHistory>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}