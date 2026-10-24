using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class DashboardRepository(AppDbContext context) : IDashboardRepository
{
    public Task<int> CountTrainingByOwnerAsync(int employeeId, IReadOnlyCollection<TrainingStatus> statuses) =>
        context.TrainingRequests.CountAsync(t => t.EmployeeId == employeeId && statuses.Contains(t.Status));

    public Task<int> CountIncompleteCoursesAsync(int employeeId) =>
        context.CourseAssignments.CountAsync(a => a.EmployeeId == employeeId && a.Status != AssignmentStatus.Completed);

    public Task<int> CountOverdueCoursesAsync(int employeeId, DateOnly today) =>
        context.CourseAssignments.CountAsync(a => a.EmployeeId == employeeId
                                                  && a.Status != AssignmentStatus.Completed
                                                  && a.DueDate < today);

    public Task<int> CountKpiPlansAwaitingActionAsync(int employeeId, DateOnly today) =>
        context.KpiPlans.CountAsync(p => p.EmployeeId == employeeId
                                         && (p.Status == KpiPlanStatus.Active || p.Status == KpiPlanStatus.Returned)
                                         && p.EffectiveDeadline >= today);

    public Task<int> CountActiveApplicationsAsync(int employeeId) =>
        context.OpportunityApplications.CountAsync(a => a.EmployeeId == employeeId
                                                        && (a.Status == ApplicationStatus.Submitted
                                                            || a.Status == ApplicationStatus.Shortlisted));

    public Task<int> CountUnreadNotificationsAsync(int employeeId) =>
        context.Notifications.CountAsync(n => n.EmployeeId == employeeId && !n.IsRead);

    public Task<int> CountPublishedPayslipPeriodsAsync(int employeeId, int year) =>
        context.Payslips.CountAsync(p => p.EmployeeId == employeeId
                                         && p.Year == year
                                         && p.Versions.Any(v => v.Status == PayslipVersionStatus.Published));

    public Task<int> CountManagerTrainingReviewsAsync(int reviewerId) =>
        context.TrainingRequests.CountAsync(t => t.AssignedReviewerId == reviewerId
                                                 && t.Status == TrainingStatus.PendingManagerApproval
                                                 && t.EmployeeId != reviewerId);

    public Task<int> CountKpiReviewsAsync(int reviewerId) =>
        context.KpiPlans.CountAsync(p => p.AssignedReviewerId == reviewerId
                                         && p.Status == KpiPlanStatus.Submitted
                                         && p.EmployeeId != reviewerId);
}
