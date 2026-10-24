using EmployeeSelfServiceAPI.Domain.Entities.Identity;

namespace EmployeeSelfServiceAPI.Domain.Entities.Common;

public class AuditLog
{
    public int Id { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TargetEntity { get; set; }
    public int? TargetId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}