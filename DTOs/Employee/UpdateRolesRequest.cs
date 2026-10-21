using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.DTOs.Employee;

public class UpdateRolesRequest
{
    public List<UserRole> Roles { get; set; }
}