using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.DTOs.Employee;

public class EmployeeProfileResponse
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool HasCompletedSetup { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int GradeId { get; set; }
    public string GradeCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public int? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }
    public List<UserRole> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}