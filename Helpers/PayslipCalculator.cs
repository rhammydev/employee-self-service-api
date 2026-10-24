namespace EmployeeSelfServiceAPI.Helpers;

public static class PayslipCalculator
{
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static (decimal Allowances, decimal Deductions, decimal Gross, decimal Net) Totals(
        decimal basicSalary, IEnumerable<decimal> allowances, IEnumerable<decimal> deductions)
    {
        var totalAllowances = Round(allowances.Sum(Round));
        var totalDeductions = Round(deductions.Sum(Round));
        var gross = Round(Round(basicSalary) + totalAllowances);
        var net = Round(gross - totalDeductions);
        return (totalAllowances, totalDeductions, gross, net);
    }
}
