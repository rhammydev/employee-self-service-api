using System.Security.Claims;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetEmployeeId(this ClaimsPrincipal user) => 
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    public static IReadOnlyList<UserRole> GetRoles(this ClaimsPrincipal user) => 
        user.FindAll(ClaimTypes.Role).Select(x => Enum.Parse<UserRole>(x.Value)).ToList();
}