namespace BlueMoon.API.Models;

public class Payment
{
    public int Id { get; set; }
    public int ApartmentId { get; set; }
    public int? ResidentId { get; set; }
    public int ReceivedByUserId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Note { get; set; }

    public Apartment? Apartment { get; set; }
    public Resident? Resident { get; set; }
    public ICollection<PaymentDetail> PaymentDetails { get; set; } = new List<PaymentDetail>();
}