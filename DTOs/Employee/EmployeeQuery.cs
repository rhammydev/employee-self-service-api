using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Employee;

public class EmployeeQuery : PagedRequest
{
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
}