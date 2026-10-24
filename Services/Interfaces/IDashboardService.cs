using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse> GetMineAsync(int employeeId, IReadOnlyList<UserRole> roles);
}
