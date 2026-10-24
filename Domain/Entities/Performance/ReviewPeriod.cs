using EmployeeSelfServiceAPI.Domain.Entities.Identity;

namespace EmployeeSelfServiceAPI.Domain.Entities.Performance;

public class ReviewPeriod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly SubmissionDeadline { get; set; }
    public int CreatedById { get; set; }
    public Employee? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<KpiPlan> Plans { get; set; } = new List<KpiPlan>();
}