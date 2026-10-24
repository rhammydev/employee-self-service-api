using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Notification;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(
        Employee recipient,
        string title,
        string message,
        string? emailBody = null,
        string? relatedEntity = null,
        int? relatedEntityId = null
    );

    Task<ApiResponse> GetMineAsync(int employeeId, NotificationQuery query);
    Task<ApiResponse> MarkReadAsync(int employeeId, int notificationId);
    Task<ApiResponse> MarkAllReadAsync(int employeeId);
}
