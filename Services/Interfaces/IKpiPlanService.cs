using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Performance;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IKpiPlanService
{
    Task<ApiResponse> CreatePeriodAsync(int hrId, CreateReviewPeriodRequest request);
    Task<ApiResponse> GetPeriodsAsync(PagedRequest request);
    Task<ApiResponse> CreatePlanAsync(int hrId, CreateKpiPlanRequest request);
    Task<ApiResponse> GetAllAsync(KpiPlanQuery query);
    Task<ApiResponse> AddItemAsync(int hrId, int planId, SaveKpiItemRequest request);
    Task<ApiResponse> UpdateItemAsync(int hrId, int planId, int itemId, SaveKpiItemRequest request);
    Task<ApiResponse> DeleteItemAsync(int hrId, int planId, int itemId);
    Task<ApiResponse> ActivateAsync(int hrId, int planId, ActivateKpiPlanRequest request);
    Task<ApiResponse> ReopenAsync(int hrId, int planId, ReopenKpiPlanRequest request);
}
