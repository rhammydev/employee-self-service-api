using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Identity;

public class AccountToken
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public TokenPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? InvalidatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}