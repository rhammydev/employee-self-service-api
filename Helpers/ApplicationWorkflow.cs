using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Helpers;

public static class ApplicationWorkflow
{
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> HrTransitions = new()
    {
        [ApplicationStatus.Submitted] = [ApplicationStatus.Shortlisted, ApplicationStatus.Rejected],
        [ApplicationStatus.Shortlisted] = [ApplicationStatus.Selected, ApplicationStatus.Rejected]
    };

    public static bool CanHrMove(ApplicationStatus from, ApplicationStatus to) =>
        HrTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool CanWithdraw(ApplicationStatus status) =>
        status is ApplicationStatus.Submitted or ApplicationStatus.Shortlisted;

    public static bool IsEligible(IReadOnlyCollection<int> allowedDepartmentIds, IReadOnlyCollection<int> allowedGradeIds,
        int departmentId, int gradeId) =>
        (allowedDepartmentIds.Count == 0 || allowedDepartmentIds.Contains(departmentId))
        && (allowedGradeIds.Count == 0 || allowedGradeIds.Contains(gradeId));
}
