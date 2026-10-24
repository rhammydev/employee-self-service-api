namespace EmployeeSelfServiceAPI.Helpers;

public static class AssessmentScorer
{
    public const int MaxAttemptsPerCycle = 3;  

    public static decimal Score(int correctCount, int totalQuestions) =>
        totalQuestions == 0 ? 0 : correctCount * 100m / totalQuestions;

    public static bool IsPass(decimal score, int passMark) => score >= passMark;
}
