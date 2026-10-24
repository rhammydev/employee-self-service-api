using EmployeeSelfServiceAPI.Domain.Entities.Identity;

namespace EmployeeSelfServiceAPI.Domain.Entities.Opportunities;

public class OpportunityAllowedGrade
{
    public int OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }
    public int GradeId { get; set; }
    public Grade? Grade { get; set; }
}