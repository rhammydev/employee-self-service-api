using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.DTOs.Payroll;

public class PayslipSummaryResponse
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public int? CurrentVersion { get; set; }
    public decimal? NetPay { get; set; }
    public bool HasPendingDraft { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class PayslipDetailResponse
{
    public PayslipSummaryResponse Summary { get; set; } = new();
    public List<PayslipVersionResponse> Versions { get; set; } = [];
}

public class PayslipVersionResponse
{
    public int VersionNumber { get; set; }
    public PayslipVersionStatus Status { get; set; }
    public string Currency { get; set; } = "NGN";
    public decimal BasicSalary { get; set; }
    public List<PayslipLineResponse> Allowances { get; set; } = [];
    public List<PayslipLineResponse> Deductions { get; set; } = [];
    public decimal TotalAllowances { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public string? RevisionReason { get; set; }
    public string PreparedByName { get; set; } = string.Empty;
    public string? PublishedByName { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? SupersededAt { get; set; }
    public string? PdfUrl { get; set; }
}

public class PayslipLineResponse
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
