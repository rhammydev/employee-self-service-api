using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Opportunities;

public class Opportunity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public OpportunityType Type { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Eligibility { get; set; }
    public DateTime ClosingAt { get; set; }
    public OpportunityStatus Status { get; set; }
}