using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Employee;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id);
    Task<List<UserRole>?> GetActiveRolesAsync(int id);
    Task<Employee?> GetByEmailWithRolesAsync(string email);
    Task<Employee?> GetByIdWithRolesAsync(int id);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> EmployeeNumberExistsAsync(string employeeNumber);
    Task<bool> HasDirectReportsAsync(int managerId);
    Task<int?> GetReportingManagerIdAsync(int id);
    Task<Employee?> GetDetailsAsync(int id);   // with Department, Grade, ReportingManager, Roles
    Task<PagedResult<Employee>> GetPagedAsync(EmployeeQuery query);
    Task<PagedResult<Employee>> GetDirectReportsPagedAsync(int managerId, PagedRequest request);
    Task AddAsync(Employee employee);
    Task<List<Employee>> GetActiveByRoleAsync(UserRole role);
    void RemoveRole(EmployeeRole role);
}