using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class NotificationService(INotificationRepository notificationRepository, IEmailQueueRepository emailQueueRepository): INotificationService
{
    public async Task NotifyAsync(Employee recipient, string title, string message, string? emailBody = null,
        string? relatedEntity = null, int? relatedEntityId = null)
    {
        var now = DateTime.UtcNow;

        await notificationRepository.AddAsync(new Notification
        {
            Employee = recipient,
            Title = title,
            Message = message,
            RelatedEntity = relatedEntity,
            RelatedEntityId = relatedEntityId,
            CreatedAt = now,
        });

        await emailQueueRepository.AddAsync(new EmailMessage
        {
            Employee = recipient,
            Recipient = recipient.Email,
            Subject = title,
            Body = emailBody ?? message,
            Status = EmailStatus.Pending,
            NextAttemptAt =  now,
            CreatedAt = now,
        });
    }
}