using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class OrganizationRepository(AppDbContext context) : IOrganizationRepository
{
    public Task<bool> DepartmentExistsAsync(int id) => context.Departments.AnyAsync(d => d.Id == id);

    public Task<bool> GradeExistsAsync(int id) => context.Grades.AnyAsync(g => g.Id == id);
}