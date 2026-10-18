using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Opportunities;

public class ApplicationStatusHistory
{
    public int Id { get; set; }
    public int OpportunityApplicationId { get; set; }
    public OpportunityApplication? OpportunityApplication { get; set; }
    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }
    public int ActorId { get; set; } 
    public Employee? Actor { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}