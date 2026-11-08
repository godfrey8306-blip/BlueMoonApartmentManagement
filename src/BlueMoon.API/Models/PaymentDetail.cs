namespace BlueMoon.API.Models;

public class PaymentDetail
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public int FeeChargeId { get; set; }
    public decimal Amount { get; set; }

    public Payment? Payment { get; set; }
    public FeeCharge? FeeCharge { get; set; }
}