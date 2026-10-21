using EmployeeSelfServiceAPI.Domain.Entities.Identity;

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
}