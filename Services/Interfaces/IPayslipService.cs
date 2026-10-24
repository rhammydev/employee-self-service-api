using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Payroll;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IPayslipService
{
    Task<ApiResponse> CreateAsync(int financeId, CreatePayslipRequest request);
    Task<ApiResponse> UpdateDraftAsync(int financeId, int payslipId, PayslipAmountsRequest request);
    Task<ApiResponse> CreateRevisionAsync(int financeId, int payslipId, CreateRevisionRequest request);
    Task<ApiResponse> PublishAsync(int financeId, int payslipId);
    Task<ApiResponse> GetAllAsync(PayslipQuery query);
    Task<ApiResponse> GetMineAsync(int employeeId, PayslipQuery query);
    Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int payslipId);
    Task<(ApiResponse? Error, FileDownload? File)> GetPdfAsync(int userId, IReadOnlyList<UserRole> roles, int payslipId, int? versionNumber);
}
