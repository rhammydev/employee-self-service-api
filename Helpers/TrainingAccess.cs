using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Helpers;

public static class TrainingAccess
{
    public static bool CanView(TrainingRequest request, int userId, IReadOnlyCollection<UserRole> roles)
    {
        if(request.EmployeeId == userId) return true;
        if (request.Status == TrainingStatus.Draft) return false;
        
        return request.AssignedReviewerId == userId                         
               || roles.Contains(UserRole.HR)
               || roles.Contains(UserRole.Admin)
               || (roles.Contains(UserRole.Finance) && request.Allowances.Count > 0);
    }
}