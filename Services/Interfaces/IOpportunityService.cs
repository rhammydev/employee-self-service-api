using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Opportunities;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IOpportunityService
{
    Task<ApiResponse> CreateAsync(int hrId, SaveOpportunityRequest request);
    Task<ApiResponse> UpdateAsync(int opportunityId, SaveOpportunityRequest request);
    Task<ApiResponse> PublishAsync(int opportunityId);
    Task<ApiResponse> CloseAsync(int opportunityId);
    Task<ApiResponse> GetAllAsync(int userId, IReadOnlyList<UserRole> roles, OpportunityQuery query);
    Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int opportunityId);
    Task<ApiResponse> GetApplicationsAsync(int opportunityId, ApplicationQuery query);

    Task<ApiResponse> ApplyAsync(int employeeId, int opportunityId, ApplyRequest request);
    Task<ApiResponse> GetMyApplicationsAsync(int employeeId, ApplicationQuery query);
    Task<ApiResponse> GetApplicationAsync(int userId, IReadOnlyList<UserRole> roles, int applicationId);
    Task<ApiResponse> WithdrawAsync(int employeeId, int applicationId);
    Task<ApiResponse> UpdateApplicationStatusAsync(int hrId, int applicationId, UpdateApplicationStatusRequest request);
}
