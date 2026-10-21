using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.DTOs.Auth;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse> LoginAsync(LoginRequest request);
    Task<ApiResponse> SetupPasswordAsync(SetPasswordRequest request);
    Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse> ResetPasswordAsync(SetPasswordRequest request);
    Task<ApiResponse> ChangePasswordAsync(int employeeId, ChangePasswordRequest request);

    Task IssueSetupLinkAsync(Employee employee);
}