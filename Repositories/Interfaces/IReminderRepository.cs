using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Entities.Performance;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IReminderRepository
{
    Task<List<CourseAssignment>> GetCourseAssignmentsNeedingReminderAsync(DateOnly today, DateOnly dueOn);

    Task<List<KpiPlan>> GetKpiPlansWithDeadlineOnAsync(IReadOnlyCollection<DateOnly> deadlines);

    Task<HashSet<string>> GetSentKeysAsync(DateOnly reminderDate);

    Task AddLogAsync(ReminderLog log);
}
