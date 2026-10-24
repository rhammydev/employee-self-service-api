using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Payroll;

public class PayslipLineItem
{
    public int Id { get; set; }
    public int PayslipVersionId { get; set; }
    public PayslipVersion? PayslipVersion { get; set; }
    public PayslipLineType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}