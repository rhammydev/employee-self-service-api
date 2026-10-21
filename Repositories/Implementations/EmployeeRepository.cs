using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(int id)
    {
        return await context.Employees.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<UserRole>?> GetActiveRolesAsync(int id)
    {
        var employee = await context.Employees.AsNoTracking()
            .Where(e => e.Id == id && e.IsActive)
            .Select(e => new
            {
                Roles = e.Roles.Select(r => r.Role).ToList()
            })
            .FirstOrDefaultAsync();

        return employee?.Roles;
    }
}