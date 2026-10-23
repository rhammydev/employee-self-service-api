using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Training;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface ITrainingApprovalService
{
    Task<ApiResponse> GetPendingHrAsync(PagedRequest request);
    Task<ApiResponse> HrApproveAsync(int hrId, int id);
    Task<ApiResponse> CorrectAllowanceAsync(int hrId, int id, AllowanceCorrectionRequest request);
    Task<ApiResponse> GetPendingFinanceAsync(PagedRequest request);
    Task<ApiResponse> FinanceDecisionAsync(int financeId, int id, FinanceDecisionRequest request);
    Task<(ApiResponse? Error, FileDownload? File)> GetAllowancePdfAsync(int userId, IReadOnlyList<UserRole> roles, int id);
}
