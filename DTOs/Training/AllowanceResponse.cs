namespace EmployeeSelfServiceAPI.DTOs.Training;

public class AllowanceResponse
{
    public int VersionNumber { get; set; }
    public bool IsCurrent { get; set; }
    public string GradeCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int ComputedEligibleDays { get; set; }
    public int EligibleDays { get; set; }
    public decimal Amount { get; set; }
    public bool IsRateOverridden { get; set; }
    public string? CorrectionReason { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
