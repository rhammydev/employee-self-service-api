using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class ReminderRepository(AppDbContext context) : IReminderRepository
{
    public Task<List<CourseAssignment>> GetCourseAssignmentsNeedingReminderAsync(DateOnly today, DateOnly dueOn) =>
        context.CourseAssignments
            .Include(a => a.Employee)
            .Include(a => a.Course)
            .Where(a => a.Status != AssignmentStatus.Completed
                        && a.Employee!.IsActive
                        && (a.DueDate == dueOn || a.DueDate < today))
            .ToListAsync();

    public Task<List<KpiPlan>> GetKpiPlansWithDeadlineOnAsync(IReadOnlyCollection<DateOnly> deadlines) =>
        context.KpiPlans
            .Include(p => p.Employee)
            .Include(p => p.AssignedReviewer)
            .Include(p => p.ReviewPeriod)
            .Where(p => (p.Status == KpiPlanStatus.Active || p.Status == KpiPlanStatus.Returned)
                        && deadlines.Contains(p.EffectiveDeadline))
            .ToListAsync();

    public async Task<HashSet<string>> GetSentKeysAsync(DateOnly reminderDate) =>
        (await context.ReminderLogs
            .Where(r => r.ReminderDate == reminderDate)
            .Select(r => new { r.RecipientId, r.ItemType, r.ItemId, r.ReminderType })
            .ToListAsync())
        .Select(r => $"{r.RecipientId}|{r.ItemType}|{r.ItemId}|{r.ReminderType}")
        .ToHashSet();

    public async Task AddLogAsync(ReminderLog log) =>
        await context.ReminderLogs.AddAsync(log);
}
