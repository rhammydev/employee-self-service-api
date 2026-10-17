using EmployeeSelfServiceAPI.Domain.Entities.Identity;

namespace EmployeeSelfServiceAPI.Domain.Entities.Learning;

public class AssignmentReset
{
    public int Id { get; set; }
    public int CourseAssignmentId { get; set; }
    public CourseAssignment? CourseAssignment { get; set; }
    public int FromCycle { get; set; }
    public int ToCycle { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ResetById { get; set; }
    public Employee? ResetBy { get; set; }
    public DateTime ResetAt { get; set; }
}