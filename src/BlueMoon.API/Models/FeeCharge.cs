namespace BlueMoon.API.Models;

public class FeeCharge
{
    public int Id { get; set; }
    public int FeeCollectionId { get; set; }
    public int ApartmentId { get; set; }
    public int FeeCategoryId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Unpaid";

    public FeeCollection? FeeCollection { get; set; }
    public Apartment? Apartment { get; set; }
    public FeeCategory? FeeCategory { get; set; }
    public ICollection<PaymentDetail> PaymentDetails { get; set; } = new List<PaymentDetail>();
}