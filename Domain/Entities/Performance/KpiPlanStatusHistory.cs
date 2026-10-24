using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Performance;

public class KpiPlanStatusHistory
{
    public int Id { get; set; }
    public int KpiPlanId { get; set; }
    public KpiPlan? KpiPlan { get; set; }
    public KpiAction Action { get; set; }
    public KpiPlanStatus? FromStatus { get; set; }
    public KpiPlanStatus ToStatus { get; set; }
    public int ActorId { get; set; }
    public Employee? Actor { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}