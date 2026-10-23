namespace EmployeeSelfServiceAPI.DTOs.Training;

public class AllowanceCorrectionRequest
{
    public decimal? DailyRate { get; set; }
    public int? EligibleDays { get; set; }
    public string Reason { get; set; } = string.Empty;
}
