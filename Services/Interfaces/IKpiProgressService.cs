using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Performance;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IKpiProgressService
{
    Task<ApiResponse> GetMineAsync(int employeeId, KpiPlanQuery query);
    Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int planId);
    Task<ApiResponse> UpdateProgressAsync(int employeeId, int planId, int itemId, UpdateKpiProgressRequest request);
    Task<ApiResponse> UploadEvidenceAsync(int employeeId, int planId, int itemId, IFormFile file);
    Task<ApiResponse> SubmitAsync(int employeeId, int planId);
    Task<ApiResponse> GetPendingReviewAsync(int managerId, PagedRequest request);
    Task<ApiResponse> ReviewAsync(int managerId, int planId, KpiReviewRequest request);
}
