using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Payroll;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Payroll;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class PayslipRepository(AppDbContext context) : IPayslipRepository
{
    public async Task AddAsync(Payslip payslip) =>
        await context.Payslips.AddAsync(payslip);

    public Task<bool> ExistsAsync(int employeeId, int month, int year) =>
        context.Payslips.AnyAsync(p => p.EmployeeId == employeeId && p.Month == month && p.Year == year);

    public Task<Payslip?> GetForUpdateAsync(int id) =>
        context.Payslips
            .AsSplitQuery()
            .Include(p => p.Employee)
            .Include(p => p.Versions).ThenInclude(v => v.LineItems)
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<Payslip?> GetDetailsAsync(int id) =>
        context.Payslips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Employee).ThenInclude(e => e!.Department)
            .Include(p => p.Versions).ThenInclude(v => v.LineItems)
            .Include(p => p.Versions).ThenInclude(v => v.PreparedBy)
            .Include(p => p.Versions).ThenInclude(v => v.PublishedBy)
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<PagedResult<Payslip>> GetPagedAsync(PayslipQuery query) =>
        Filter(WithVersions(), query)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ThenBy(p => p.Employee!.LastName)
            .ToPagedResultAsync(query);

    public Task<PagedResult<Payslip>> GetPagedPublishedForEmployeeAsync(int employeeId, PayslipQuery query) =>
        Filter(WithVersions().Where(p => p.EmployeeId == employeeId
                                         && p.Versions.Any(v => v.Status == PayslipVersionStatus.Published)), query)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .ToPagedResultAsync(query);

    public void RemoveLineItem(PayslipLineItem item) => context.PayslipLineItems.Remove(item);

    private IQueryable<Payslip> WithVersions() =>
        context.Payslips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Employee)
            .Include(p => p.Versions);

    private static IQueryable<Payslip> Filter(IQueryable<Payslip> payslips, PayslipQuery query)
    {
        if (query.EmployeeId.HasValue)
            payslips = payslips.Where(p => p.EmployeeId == query.EmployeeId);
        if (query.Month.HasValue)
            payslips = payslips.Where(p => p.Month == query.Month);
        if (query.Year.HasValue)
            payslips = payslips.Where(p => p.Year == query.Year);
        return payslips;
    }
}
