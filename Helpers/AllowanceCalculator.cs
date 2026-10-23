namespace EmployeeSelfServiceAPI.Helpers;

public static class AllowanceCalculator
{
    // both travel days count
    public static int EligibleDays(DateOnly departureDate, DateOnly returnDate) =>
        returnDate.DayNumber - departureDate.DayNumber + 1;

    public static decimal Amount(int eligibleDays, decimal dailyRate) =>
        Math.Round(eligibleDays * dailyRate, 2, MidpointRounding.AwayFromZero);
}
