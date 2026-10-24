using EmployeeSelfServiceAPI.Domain.Entities.Payroll;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Payroll;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IPayslipRepository
{
    Task AddAsync(Payslip payslip);
    Task<bool> ExistsAsync(int employeeId, int month, int year);

    Task<Payslip?> GetForUpdateAsync(int id);

    Task<Payslip?> GetDetailsAsync(int id);

    Task<PagedResult<Payslip>> GetPagedAsync(PayslipQuery query);

    Task<PagedResult<Payslip>> GetPagedPublishedForEmployeeAsync(int employeeId, PayslipQuery query);

    void RemoveLineItem(PayslipLineItem item);
}
