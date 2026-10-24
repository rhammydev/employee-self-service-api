using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Identity;

public class EmployeeRole
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public UserRole Role { get; set; }
    public int? AssignedById { get; set; }
    public DateTime AssignedAt { get; set; }
}