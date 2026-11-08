namespace BlueMoon.API.Models;

public class FeeCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FeeCharge> FeeCharges { get; set; } = new List<FeeCharge>();
}