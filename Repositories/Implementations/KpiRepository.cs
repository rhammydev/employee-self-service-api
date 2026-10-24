using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Performance;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class KpiRepository(AppDbContext context) : IKpiRepository
{
    public async Task AddPeriodAsync(ReviewPeriod period) =>
        await context.ReviewPeriods.AddAsync(period);

    public Task<ReviewPeriod?> GetPeriodAsync(int id) =>
        context.ReviewPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public Task<PagedResult<ReviewPeriod>> GetPagedPeriodsAsync(PagedRequest request) =>
        context.ReviewPeriods.AsNoTracking().OrderByDescending(p => p.StartDate).ToPagedResultAsync(request);

    public async Task AddPlanAsync(KpiPlan plan) =>
        await context.KpiPlans.AddAsync(plan);

    public Task<bool> PlanExistsAsync(int employeeId, int reviewPeriodId) =>
        context.KpiPlans.AnyAsync(p => p.EmployeeId == employeeId && p.ReviewPeriodId == reviewPeriodId);

    public Task<KpiPlan?> GetPlanForUpdateAsync(int id) =>
        context.KpiPlans
            .AsSplitQuery()
            .Include(p => p.Employee)
            .Include(p => p.AssignedReviewer)
            .Include(p => p.ReviewPeriod)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<KpiPlan?> GetPlanDetailsAsync(int id) =>
        context.KpiPlans
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Employee)
            .Include(p => p.AssignedReviewer)
            .Include(p => p.ReviewPeriod)
            .Include(p => p.Items)
            .Include(p => p.Reopenings).ThenInclude(r => r.ReopenedBy)
            .Include(p => p.StatusHistory).ThenInclude(h => h.Actor)
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<KpiPlan?> GetPlanByItemIdAsync(int itemId) =>
        context.KpiPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Items.Any(i => i.Id == itemId));

    public Task<PagedResult<KpiPlan>> GetPagedPlansAsync(KpiPlanQuery query) =>
        Filter(WithSummaryData(), query).OrderByDescending(p => p.CreatedAt).ToPagedResultAsync(query);

    public Task<PagedResult<KpiPlan>> GetPagedPlansForEmployeeAsync(int employeeId, KpiPlanQuery query) =>
        // Drafts are HR's work in progress; the employee sees a plan once it is activated
        Filter(WithSummaryData().Where(p => p.EmployeeId == employeeId && p.Status != KpiPlanStatus.Draft), query)
            .OrderByDescending(p => p.CreatedAt)
            .ToPagedResultAsync(query);

    public Task<PagedResult<KpiPlan>> GetPagedPendingReviewAsync(int reviewerId, PagedRequest request) =>
        WithSummaryData()
            .Where(p => p.AssignedReviewerId == reviewerId && p.Status == KpiPlanStatus.Submitted)
            .OrderBy(p => p.SubmittedAt)
            .ToPagedResultAsync(request);

    public void RemoveItem(KpiItem item) => context.KpiItems.Remove(item);

    private IQueryable<KpiPlan> WithSummaryData() =>
        context.KpiPlans
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Employee)
            .Include(p => p.AssignedReviewer)
            .Include(p => p.ReviewPeriod)
            .Include(p => p.Items);

    private static IQueryable<KpiPlan> Filter(IQueryable<KpiPlan> plans, KpiPlanQuery query)
    {
        if (query.ReviewPeriodId.HasValue)
            plans = plans.Where(p => p.ReviewPeriodId == query.ReviewPeriodId);
        if (query.EmployeeId.HasValue)
            plans = plans.Where(p => p.EmployeeId == query.EmployeeId);
        if (query.Status.HasValue)
            plans = plans.Where(p => p.Status == query.Status);
        return plans;
    }
}
