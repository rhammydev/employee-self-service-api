using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IDashboardRepository
{
    Task<int> CountTrainingByOwnerAsync(int employeeId, IReadOnlyCollection<TrainingStatus> statuses);
    Task<int> CountIncompleteCoursesAsync(int employeeId);
    Task<int> CountOverdueCoursesAsync(int employeeId, DateOnly today);
    Task<int> CountKpiPlansAwaitingActionAsync(int employeeId, DateOnly today);
    Task<int> CountActiveApplicationsAsync(int employeeId);
    Task<int> CountUnreadNotificationsAsync(int employeeId);
    Task<int> CountPublishedPayslipPeriodsAsync(int employeeId, int year);
    Task<int> CountManagerTrainingReviewsAsync(int reviewerId);
    Task<int> CountKpiReviewsAsync(int reviewerId);
}
