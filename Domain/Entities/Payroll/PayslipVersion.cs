using System.ComponentModel.DataAnnotations;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Payroll;

public class PayslipVersion
{
    public int Id { get; set; }
    public int PayslipId { get; set; }
    public Payslip? Payslip { get; set; }
    public int VersionNumber { get; set; }
    public PayslipVersionStatus Status { get; set; }

    public string Currency { get; set; } = "NGN";
    public decimal BasicSalary { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal GrossPay { get; set; }  // basic + allowances
    public decimal NetPay { get; set; } // gross - deductions

    public string? RevisionReason { get; set; }

    public int PreparedById { get; set; } // Finance
    public Employee? PreparedBy { get; set; }
    public int? PublishedById { get; set; }
    public Employee? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? SupersededAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<PayslipLineItem> LineItems { get; set; } = new List<PayslipLineItem>();
}