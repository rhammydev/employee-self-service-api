namespace EmployeeSelfServiceAPI.DTOs.Employee;

public class UpdateEmploymentRequest
{
    public int DepartmentId { get; set; }
    public int GradeId { get; set; }
    public int? ReportingManagerId { get; set; }
}