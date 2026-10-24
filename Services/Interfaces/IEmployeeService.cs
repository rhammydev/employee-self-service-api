using EmployeeSelfServiceAPI.DTOs.Employee;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IEmployeeService
{
    Task<ApiResponse> CreateAsync(int actorId, CreateEmployeeRequest request);
    Task<ApiResponse> ResendSetupLinkAsync(int actorId, int employeeId);
    Task<ApiResponse> UpdateRolesAsync(int actorId, int employeeId, UpdateRolesRequest request);
    Task<ApiResponse> UpdateEmploymentAsync(int actorId, int employeeId, UpdateEmploymentRequest request);
    Task<ApiResponse> UpdateStatusAsync(int actorId, int employeeId, UpdateStatusRequest request);
    Task<ApiResponse> GetAllAsync(EmployeeQuery query);
    Task<ApiResponse> GetByIdAsync(int employeeId);
    Task<ApiResponse> GetMyProfileAsync(int employeeId);
    Task<ApiResponse> UpdateMyPhoneAsync(int employeeId, UpdatePhoneRequest request);
    Task<ApiResponse> GetMyTeamAsync(int managerId, PagedRequest request);
}