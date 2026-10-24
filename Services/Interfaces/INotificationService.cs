using EmployeeSelfServiceAPI.Domain.Entities.Identity;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(
        Employee recipient,
        string title,
        string message,
        string? emailBody = null,          // HTML from MailUtils; null = MailUtils.Notification(message)
        string? relatedEntity = null,
        int? relatedEntityId = null
    );
}