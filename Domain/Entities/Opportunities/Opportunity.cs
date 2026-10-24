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
    
    public int CreatedById { get; set; }
    public Employee? CreatedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<OpportunityAllowedDepartment> AllowedDepartments { get; set; } = new List<OpportunityAllowedDepartment>();
    public ICollection<OpportunityAllowedGrade> AllowedGrades { get; set; } = new List<OpportunityAllowedGrade>();
    public ICollection<OpportunityApplication> Applications { get; set; } = new List<OpportunityApplication>();
}