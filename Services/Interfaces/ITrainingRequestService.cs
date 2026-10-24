using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Training;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface ITrainingRequestService
{
    Task<ApiResponse> CreateAsync(int employeeId, SaveTrainingRequest request);
    Task<ApiResponse> UpdateAsync(int employeeId, int id, SaveTrainingRequest request);
    Task<ApiResponse> UploadSupportingFileAsync(int employeeId, int id, IFormFile file);
    Task<ApiResponse> SubmitAsync(int employeeId, int id);
    Task<ApiResponse> SubmitEvidenceAsync(int employeeId, int id, IFormFile file);
    Task<ApiResponse> WithdrawAsync(int employeeId, int id, ReasonRequest request);
    Task<ApiResponse> ManagerDecisionAsync(int managerId, int id, ManagerDecisionRequest request);
    Task<ApiResponse> ReassignReviewerAsync(int adminId, int id, ReassignReviewerRequest request);
    Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int id);
    Task<ApiResponse> GetMineAsync(int employeeId, TrainingRequestQuery query);
    Task<ApiResponse> GetPendingManagerAsync(int managerId, PagedRequest request);
    Task<ApiResponse> GetAllPendingManagerAsync(PagedRequest request);
}