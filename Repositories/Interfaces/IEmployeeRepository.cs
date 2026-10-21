using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id);
    Task<List<UserRole>?> GetActiveRolesAsync(int id);
}