using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Common;

public class EmailMessage
{
    public int Id { get; set; }
    public int? EmployeeId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public string? ClaimedBy { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}