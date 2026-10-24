namespace EmployeeSelfServiceAPI.DTOs.Employee;

public class CreateEmployeeRequest
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int DepartmentId { get; set; }
    public int GradeId { get; set; }
    public int? ReportingManagerId { get; set; }
}