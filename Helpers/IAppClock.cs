namespace EmployeeSelfServiceAPI.Helpers;

public interface IAppClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; } 
    DateOnly ToAppDate(DateTime utc);
}