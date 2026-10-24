using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class OrganizationRepository(AppDbContext context) : IOrganizationRepository
{
    public Task<bool> DepartmentExistsAsync(int id) => context.Departments.AnyAsync(d => d.Id == id);
    public Task<bool> GradeExistsAsync(int id) => context.Grades.AnyAsync(g => g.Id == id);

    public async Task<List<int>> GetMissingDepartmentIdsAsync(IReadOnlyCollection<int> ids)
    {
        var existing = await context.Departments.Where(d => ids.Contains(d.Id)).Select(d => d.Id).ToListAsync();
        return ids.Except(existing).ToList();
    }

    public async Task<List<int>> GetMissingGradeIdsAsync(IReadOnlyCollection<int> ids)
    {
        var existing = await context.Grades.Where(g => ids.Contains(g.Id)).Select(g => g.Id).ToListAsync();
        return ids.Except(existing).ToList();
    }
}
