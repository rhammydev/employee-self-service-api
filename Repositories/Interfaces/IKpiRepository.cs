using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Performance;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IKpiRepository
{
    // Review periods
    Task AddPeriodAsync(ReviewPeriod period);
    Task<ReviewPeriod?> GetPeriodAsync(int id);
    Task<PagedResult<ReviewPeriod>> GetPagedPeriodsAsync(PagedRequest request);

    // Plans
    Task AddPlanAsync(KpiPlan plan);
    Task<bool> PlanExistsAsync(int employeeId, int reviewPeriodId);

    Task<KpiPlan?> GetPlanForUpdateAsync(int id);

    Task<KpiPlan?> GetPlanDetailsAsync(int id);

    Task<KpiPlan?> GetPlanByItemIdAsync(int itemId);

    Task<PagedResult<KpiPlan>> GetPagedPlansAsync(KpiPlanQuery query);
    Task<PagedResult<KpiPlan>> GetPagedPlansForEmployeeAsync(int employeeId, KpiPlanQuery query);
    Task<PagedResult<KpiPlan>> GetPagedPendingReviewAsync(int reviewerId, PagedRequest request);

    void RemoveItem(KpiItem item);
}
