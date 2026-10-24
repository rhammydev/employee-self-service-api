using System.ComponentModel.DataAnnotations;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Opportunities;

public class OpportunityApplication
{
    public int Id { get; set; }
    public int OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string SupportingStatement { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }

    public int DepartmentIdAtApply { get; set; }
    public Department? DepartmentAtApply { get; set; }
    public int GradeIdAtApply { get; set; }
    public Grade? GradeAtApply { get; set; }
    public DateTime EligibilityCheckedAt { get; set; }

    public DateTime SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
}