using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Dashboard;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class DashboardService(IDashboardRepository dashboardRepository, IAppClock clock) : IDashboardService
{
    private static readonly TrainingStatus[] PendingTrainingStatuses =
    [
        TrainingStatus.PendingManagerApproval, TrainingStatus.PendingHRApproval,
        TrainingStatus.PendingFinanceApproval, TrainingStatus.ReturnedToHR
    ];

    public async Task<ApiResponse> GetMineAsync(int employeeId, IReadOnlyList<UserRole> roles)
    {
        var today = clock.Today;
        var isManager = roles.Contains(UserRole.Manager);

        // Sequential, not Task.WhenAll: one DbContext cannot run queries in parallel
        var response = new DashboardResponse
        {
            PendingTraining = await dashboardRepository.CountTrainingByOwnerAsync(employeeId, PendingTrainingStatuses),
            IncompleteCourses = await dashboardRepository.CountIncompleteCoursesAsync(employeeId),
            OverdueCourses = await dashboardRepository.CountOverdueCoursesAsync(employeeId, today),
            KpiPlansAwaitingAction = await dashboardRepository.CountKpiPlansAwaitingActionAsync(employeeId, today),
            ActiveApplications = await dashboardRepository.CountActiveApplicationsAsync(employeeId),
            UnreadNotifications = await dashboardRepository.CountUnreadNotificationsAsync(employeeId),
            PublishedPayslipPeriods = await dashboardRepository.CountPublishedPayslipPeriodsAsync(employeeId, today.Year),
            ManagerTrainingReviews = isManager ? await dashboardRepository.CountManagerTrainingReviewsAsync(employeeId) : null,
            KpiReviews = isManager ? await dashboardRepository.CountKpiReviewsAsync(employeeId) : null,
            GeneratedAt = clock.UtcNow
        };

        return ApiResponse.Success("Dashboard retrieved.", response);
    }
}
