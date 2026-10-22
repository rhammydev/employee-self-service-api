namespace EmployeeSelfServiceAPI.Helpers;

public class AppClock : IAppClock
{
    private static readonly TimeZoneInfo AppTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Lagos");

    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => ToAppDate(UtcNow);
    
    public DateOnly ToAppDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeToUtc(utc, AppTimeZone));
}