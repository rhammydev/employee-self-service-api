using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Entities.Opportunities;
using EmployeeSelfServiceAPI.Domain.Entities.Payroll;
using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Notification;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;
using EmployeeSelfServiceAPI.Utilities;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class NotificationService(
    INotificationRepository notificationRepository,
    IEmailQueueRepository emailQueueRepository,
    IUnitOfWork unitOfWork,
    IAppClock clock) : INotificationService
{
    public async Task NotifyAsync(Employee recipient, string title, string message, string? emailBody = null,
        string? relatedEntity = null, int? relatedEntityId = null)
    {
        var now = clock.UtcNow;

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
            Body = emailBody ?? MailUtils.Notification(recipient.FirstName, title, message, EyebrowFor(relatedEntity)),
            Status = EmailStatus.Pending,
            NextAttemptAt = now,
            CreatedAt = now,
        });
    }

    public async Task<ApiResponse> GetMineAsync(int employeeId, NotificationQuery query)
    {
        var page = await notificationRepository.GetPagedForEmployeeAsync(employeeId, query);
        return ApiResponse.Paged("Notifications retrieved.", new PagedResult<NotificationResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(n => new NotificationResponse
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                RelatedEntity = n.RelatedEntity,
                RelatedEntityId = n.RelatedEntityId,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            }).ToList()
        });
    }

    public async Task<ApiResponse> MarkReadAsync(int employeeId, int notificationId)
    {
        var notification = await notificationRepository.GetByIdAsync(notificationId);
        if (notification == null)
            return ApiResponse.NotFound("Notification not found.");
        if (notification.EmployeeId != employeeId)
            return ApiResponse.Forbidden("You can only mark your own notifications as read.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = clock.UtcNow;
            await unitOfWork.SaveChangesAsync();
        }

        return ApiResponse.Success("Notification marked as read.");
    }

    public async Task<ApiResponse> MarkAllReadAsync(int employeeId)
    {
        var count = await notificationRepository.MarkAllReadAsync(employeeId, clock.UtcNow);
        return ApiResponse.Success($"{count} notification(s) marked as read.", new { MarkedCount = count });
    }

    private static string EyebrowFor(string? relatedEntity) => relatedEntity switch
    {
        nameof(TrainingRequest) => "Training request",
        nameof(CourseAssignment) or nameof(Course) => "Learning",
        nameof(KpiPlan) => "Performance",
        nameof(Payslip) => "Payslip",
        nameof(OpportunityApplication) or nameof(Opportunity) => "Opportunities",
        null => "Account",
        _ => "Notification"
    };
}
