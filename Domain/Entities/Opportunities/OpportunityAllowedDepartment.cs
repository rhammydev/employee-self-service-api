using EmployeeSelfServiceAPI.Domain.Entities.Identity;

namespace EmployeeSelfServiceAPI.Domain.Entities.Opportunities;

public class OpportunityAllowedDepartment
{
    public int OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
}