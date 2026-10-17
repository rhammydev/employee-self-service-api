using EmployeeSelfServiceAPI.Domain.Entities.Identity;

namespace EmployeeSelfServiceAPI.Domain.Entities.Performance;

public class KpiDeadlineReopening
{
    public int Id { get; set; }
    public int KpiPlanId { get; set; }
    public KpiPlan? KpiPlan { get; set; }
    public DateOnly PreviousDeadline { get; set; }
    public DateOnly NewDeadline { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ReopenedById { get; set; }
    public Employee? ReopenedBy { get; set; }
    public DateTime ReopenedAt { get; set; }
}