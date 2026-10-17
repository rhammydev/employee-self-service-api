using System.ComponentModel.DataAnnotations;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Performance;

public class KpiPlan
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int ReviewPeriodId { get; set; }
    public ReviewPeriod? ReviewPeriod { get; set; }

    public KpiPlanStatus Status { get; set; }

    public int? AssignedReviewerId { get; set; }
    public Employee? AssignedReviewer { get; set; }

    public DateOnly EffectiveDeadline { get; set; } 

    public decimal? OverallScore { get; set; } 

    public int CreatedById { get; set; }  
    public Employee? CreatedBy { get; set; }
    public int? ActivatedById { get; set; }
    public Employee? ActivatedBy { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<KpiItem> Items { get; set; } = new List<KpiItem>();
    public ICollection<KpiDeadlineReopening> Reopenings { get; set; } = new List<KpiDeadlineReopening>();
    public ICollection<KpiPlanStatusHistory> StatusHistory { get; set; } = new List<KpiPlanStatusHistory>();
}