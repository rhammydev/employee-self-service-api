using EmployeeSelfServiceAPI.Domain.Entities.Identity;

namespace EmployeeSelfServiceAPI.Domain.Entities.Training;

public class EstacodeAllowance
{
    public int Id { get; set; }
    public int TrainingRequestId { get; set; }
    public TrainingRequest? TrainingRequest { get; set; }
    public int VersionNumber { get; set; }
    
    public string GradeCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public string Currency { get; set; } = "USD";
    public int ComputedEligibleDays { get; set; }                // (return - departure).Days + 1
    public int EligibleDays { get; set; }
    public decimal Amount { get; set; }                          // EligibleDays * DailyRate

    public bool IsRateOverridden { get; set; }
    public string? CorrectionReason { get; set; }   

    public int CreatedById { get; set; }                         // HR user
    public Employee? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? ApprovedById { get; set; }                       // Finance
    public Employee? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}