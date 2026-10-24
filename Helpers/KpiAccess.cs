using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Helpers;

public static class KpiAccess
{
    public static bool CanView(KpiPlan plan, int userId, IReadOnlyCollection<UserRole> roles) =>
        roles.Contains(UserRole.HR)
        || (plan.Status != KpiPlanStatus.Draft && (plan.EmployeeId == userId || plan.AssignedReviewerId == userId));
}
