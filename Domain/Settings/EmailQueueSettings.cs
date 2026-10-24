namespace EmployeeSelfServiceAPI.Domain.Settings;

public class EmailQueueSettings
{
    public int IntervalSeconds { get; set; } = 120;
    public int BatchSize { get; set; } = 20;

    public int ClaimTimeoutMinutes { get; set; } = 10;

    public int[] RetryDelaysMinutes { get; } = [5, 15, 60];
}
