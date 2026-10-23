using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IAttachmentService
{
    Task<(ApiResponse? Error, FileDownload? File)> DownloadAsync(int userId, IReadOnlyList<UserRole> roles, int attachmentId);
}