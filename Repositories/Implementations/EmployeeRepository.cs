using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Employee;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.Extensions;
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
    
    public Task<Employee?> GetByEmailWithRolesAsync(string email) =>
        context.Employees.Include(e => e.Roles)
            .FirstOrDefaultAsync(e => e.Email == email);

    public Task<Employee?> GetByIdWithRolesAsync(int id) =>
        context.Employees.Include(e => e.Roles)
            .FirstOrDefaultAsync(e => e.Id == id);

    public Task<bool> EmailExistsAsync(string email) =>
        context.Employees.AnyAsync(e => e.Email == email);

    public Task<bool> EmployeeNumberExistsAsync(string employeeNumber) =>
        context.Employees.AnyAsync(e => e.EmployeeNumber == employeeNumber);

    public Task<bool> HasDirectReportsAsync(int managerId) =>
        context.Employees.AnyAsync(e => e.ReportingManagerId == managerId);

    public Task<int?> GetReportingManagerIdAsync(int id) =>
        context.Employees.Where(e => e.Id == id).Select(e => e.ReportingManagerId).FirstOrDefaultAsync();

    public Task<Employee?> GetDetailsAsync(int id) =>
        WithDetails(context.Employees).FirstOrDefaultAsync(e => e.Id == id);

    public Task<PagedResult<Employee>> GetPagedAsync(EmployeeQuery query)
    {
        var employees = WithDetails(context.Employees.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            employees = employees.Where(e => e.FirstName.Contains(search) || e.LastName.Contains(search)
                            || e.Email.Contains(search) || e.EmployeeNumber.Contains(search));
        }
        
        if (query.DepartmentId.HasValue)
            employees = employees.Where(e => e.DepartmentId == query.DepartmentId);
        
        if (query.IsActive.HasValue)
            employees = employees.Where(e => e.IsActive == query.IsActive);

        return employees.OrderBy(e => e.EmployeeNumber).ToPagedResultAsync(query);
    }

    public Task<PagedResult<Employee>> GetDirectReportsPagedAsync(int managerId, PagedRequest request) => 
        WithDetails(context.Employees.AsNoTracking())
            .Where(e => e.ReportingManagerId == managerId)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToPagedResultAsync(request);
    

    public async Task AddAsync(Employee employee) => await context.Employees.AddAsync(employee);
    
    public Task<List<Employee>> GetActiveByRoleAsync(UserRole role) =>
        context.Employees.Where(e => e.IsActive && e.Roles.Any(r => r.Role == role)).ToListAsync();

    public void RemoveRole(EmployeeRole role) => context.EmployeeRoles.Remove(role);

    private static IQueryable<Employee> WithDetails(IQueryable<Employee> query) =>
        query
            .Include(e => e.Roles)
            .Include(e => e.Department)
            .Include(e => e.Grade)
            .Include(e => e.ReportingManager);
}