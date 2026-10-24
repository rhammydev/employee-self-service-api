using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Performance;

public class CreateReviewPeriodRequest
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly SubmissionDeadline { get; set; } 
}

public class CreateKpiPlanRequest
{
    public int EmployeeId { get; set; }
    public int ReviewPeriodId { get; set; }
}

public class SaveKpiItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Target { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Weight { get; set; }
}

public class ActivateKpiPlanRequest
{
    public int? ReviewerId { get; set; }
}

public class UpdateKpiProgressRequest
{
    public decimal ActualValue { get; set; }
    public string? EvidenceComment { get; set; }
}

public class KpiReviewRequest
{
    public bool Approve { get; set; }
    public string? Comment { get; set; }
}

public class ReopenKpiPlanRequest
{
    public DateOnly NewDeadline { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class KpiPlanQuery : PagedRequest
{
    public int? ReviewPeriodId { get; set; }
    public int? EmployeeId { get; set; }
    public KpiPlanStatus? Status { get; set; }
}
