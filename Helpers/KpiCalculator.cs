namespace EmployeeSelfServiceAPI.Helpers;

public static class KpiCalculator
{
    public const decimal RequiredWeightTotal = 100.00m;
    public static decimal AchievementPercent(decimal actual, decimal target) =>
        target <= 0 ? 0 : Math.Min(actual / target * 100m, 100m);

    public static decimal WeightedScore(decimal achievementPercent, decimal weight) =>
        achievementPercent / 100m * weight;

    public static decimal OverallScore(IEnumerable<decimal> weightedScores) =>
        Math.Round(weightedScores.Sum(), 2, MidpointRounding.AwayFromZero);
}
