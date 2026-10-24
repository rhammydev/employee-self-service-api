using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Payroll;

public class PayslipAmountsRequest
{
    public decimal BasicSalary { get; set; }
    public List<PayslipLineRequest> Allowances { get; set; } = [];
    public List<PayslipLineRequest> Deductions { get; set; } = [];
}

public class CreatePayslipRequest : PayslipAmountsRequest
{
    public int EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public class CreateRevisionRequest : PayslipAmountsRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class PayslipLineRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PayslipQuery : PagedRequest
{
    public int? EmployeeId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}
