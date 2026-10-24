namespace EmployeeSelfServiceAPI.Services.Interfaces;

public record ReminderRunResult(DateOnly ReminderDate, int Created, int SkippedAsDuplicate);

public interface IReminderService
{
    Task<ReminderRunResult> RunAsync(CancellationToken cancellationToken);
}
