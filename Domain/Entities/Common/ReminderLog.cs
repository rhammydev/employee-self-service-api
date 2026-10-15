namespace EmployeeSelfServiceAPI.Domain.Entities.Common;

public class ReminderLog
{
    public int Id { get; set; }
    public int RecipientId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ReminderType { get; set; } = string.Empty;
    public DateOnly ReminderDate { get; set; }
    public DateTime CreatedAt { get; set; }
}