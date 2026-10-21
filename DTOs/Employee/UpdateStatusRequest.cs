namespace EmployeeSelfServiceAPI.DTOs.Employee;

public class UpdateStatusRequest
{
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
}