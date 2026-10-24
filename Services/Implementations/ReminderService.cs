using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class ReminderService(
    IReminderRepository reminderRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IAppClock clock,
    ILogger<ReminderService> logger) : IReminderService
{
    private const int KpiEarlyReminderDays = 3;   

    public async Task<ReminderRunResult> RunAsync(CancellationToken cancellationToken)
    {
        var today = clock.Today;
        var tomorrow = today.AddDays(1);
        var sentKeys = await reminderRepository.GetSentKeysAsync(today);
        var created = 0;
        var skipped = 0;

        async Task RemindAsync(Employee recipient, string itemType, int itemId, string reminderType, string title, string message)
        {
            var key = $"{recipient.Id}|{itemType}|{itemId}|{reminderType}";
            if (!sentKeys.Add(key))
            {
                skipped++;
                return;
            }

            await reminderRepository.AddLogAsync(new ReminderLog
            {
                RecipientId = recipient.Id,
                ItemType = itemType,
                ItemId = itemId,
                ReminderType = reminderType,
                ReminderDate = today,
                CreatedAt = clock.UtcNow
            });

            await notificationService.NotifyAsync(recipient, title, message, relatedEntity: itemType, relatedEntityId: itemId);
            created++;
        }

        foreach (var assignment in await reminderRepository.GetCourseAssignmentsNeedingReminderAsync(today, tomorrow))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var overdue = assignment.DueDate < today;
            await RemindAsync(assignment.Employee!, nameof(CourseAssignment), assignment.Id,
                overdue ? "CourseOverdue" : "CourseDueTomorrow",
                overdue ? "Course overdue" : "Course due tomorrow",
                overdue
                    ? $"\"{assignment.Course!.Title}\" was due on {assignment.DueDate:dd MMM yyyy} and is not complete yet."
                    : $"\"{assignment.Course!.Title}\" is due tomorrow ({assignment.DueDate:dd MMM yyyy}).");
        }

        var earlyDeadline = today.AddDays(KpiEarlyReminderDays);
        foreach (var plan in await reminderRepository.GetKpiPlansWithDeadlineOnAsync([earlyDeadline, today]))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isToday = plan.EffectiveDeadline == today;
            var reminderType = isToday ? "KpiDeadlineToday" : "KpiDeadlineIn3Days";
            var when = isToday ? "today" : $"in {KpiEarlyReminderDays} days ({plan.EffectiveDeadline:dd MMM yyyy})";
            var periodName = plan.ReviewPeriod!.Name;

            await RemindAsync(plan.Employee!, nameof(KpiPlan), plan.Id, reminderType,
                "KPI submission deadline approaching",
                $"Your KPI plan for {periodName} must be submitted {when}.");

            if (plan.AssignedReviewer is { IsActive: true } reviewer)
                await RemindAsync(reviewer, nameof(KpiPlan), plan.Id, reminderType,
                    "Team KPI deadline approaching",
                    $"{plan.Employee!.FirstName} {plan.Employee.LastName}'s KPI plan for {periodName} is due {when}.");
        }

        if (created > 0)
            await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Reminder run for {Date}: {Created} created, {Skipped} skipped as duplicates.", today, created, skipped);
        return new ReminderRunResult(today, created, skipped);
    }
}
