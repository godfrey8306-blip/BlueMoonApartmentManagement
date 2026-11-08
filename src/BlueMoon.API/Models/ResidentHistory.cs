namespace BlueMoon.API.Models;

public class ResidenceHistory
{
    public int Id { get; set; }
    public int ResidentId { get; set; }
    public string HistoryType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Destination { get; set; }
    public string? Note { get; set; }
    public int? CreatedByUserId { get; set; }

    public Resident? Resident { get; set; }
}