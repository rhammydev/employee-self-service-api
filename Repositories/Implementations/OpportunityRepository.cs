using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Opportunities;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Opportunities;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class OpportunityRepository(AppDbContext context) : IOpportunityRepository
{
    public async Task AddAsync(Opportunity opportunity) =>
        await context.Opportunities.AddAsync(opportunity);

    public Task<Opportunity?> GetForUpdateAsync(int id) =>
        context.Opportunities
            .AsSplitQuery()
            .Include(o => o.AllowedDepartments)
            .Include(o => o.AllowedGrades)
            .FirstOrDefaultAsync(o => o.Id == id);

    public Task<Opportunity?> GetDetailsAsync(int id) =>
        context.Opportunities
            .AsNoTracking()
            .AsSplitQuery()
            .Include(o => o.Department)
            .Include(o => o.AllowedDepartments).ThenInclude(a => a.Department)
            .Include(o => o.AllowedGrades).ThenInclude(a => a.Grade)
            .FirstOrDefaultAsync(o => o.Id == id);

    public Task<PagedResult<Opportunity>> GetPagedAsync(OpportunityQuery query, bool publishedOnly, DateTime now)
    {
        var opportunities = context.Opportunities.AsNoTracking().Include(o => o.Department).AsQueryable();

        // VAC-01.2: staff see only Published records; HR sees all
        if (publishedOnly)
            opportunities = opportunities.Where(o => o.Status == OpportunityStatus.Published);
        else if (query.Status.HasValue)
            opportunities = opportunities.Where(o => o.Status == query.Status);

        if (query.Type.HasValue)
            opportunities = opportunities.Where(o => o.Type == query.Type);
        if (query.OpenOnly == true)
            opportunities = opportunities.Where(o => o.Status == OpportunityStatus.Published && o.ClosingAt > now);

        return opportunities.OrderBy(o => o.ClosingAt).ToPagedResultAsync(query);
    }

    public void RemoveAllowedDepartment(OpportunityAllowedDepartment item) => context.OpportunityAllowedDepartments.Remove(item);
    public void RemoveAllowedGrade(OpportunityAllowedGrade item) => context.OpportunityAllowedGrades.Remove(item);

    public async Task AddApplicationAsync(OpportunityApplication application) =>
        await context.OpportunityApplications.AddAsync(application);

    public Task<OpportunityApplication?> GetApplicationIdForEmployeeAsync(int opportunityId, int employeeId) =>
        context.OpportunityApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.OpportunityId == opportunityId && a.EmployeeId == employeeId);

    public Task<OpportunityApplication?> GetApplicationForUpdateAsync(int id) =>
        context.OpportunityApplications
            .Include(a => a.Employee)
            .Include(a => a.Opportunity)
            .FirstOrDefaultAsync(a => a.Id == id);

    public Task<OpportunityApplication?> GetApplicationDetailsAsync(int id) =>
        context.OpportunityApplications
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Employee)
            .Include(a => a.Opportunity)
            .Include(a => a.DepartmentAtApply)
            .Include(a => a.GradeAtApply)
            .Include(a => a.StatusHistory).ThenInclude(h => h.Actor)
            .FirstOrDefaultAsync(a => a.Id == id);

    public Task<PagedResult<OpportunityApplication>> GetPagedApplicationsForOpportunityAsync(int opportunityId, ApplicationQuery query) =>
        FilterApplications(context.OpportunityApplications.Where(a => a.OpportunityId == opportunityId), query)
            .OrderBy(a => a.SubmittedAt)
            .ToPagedResultAsync(query);

    public Task<PagedResult<OpportunityApplication>> GetPagedApplicationsForEmployeeAsync(int employeeId, ApplicationQuery query) =>
        FilterApplications(context.OpportunityApplications.Where(a => a.EmployeeId == employeeId), query)
            .OrderByDescending(a => a.SubmittedAt)
            .ToPagedResultAsync(query);

    private static IQueryable<OpportunityApplication> FilterApplications(IQueryable<OpportunityApplication> applications, ApplicationQuery query)
    {
        applications = applications.AsNoTracking().Include(a => a.Employee).Include(a => a.Opportunity);
        if (query.Status.HasValue)
            applications = applications.Where(a => a.Status == query.Status);
        return applications;
    }
}
