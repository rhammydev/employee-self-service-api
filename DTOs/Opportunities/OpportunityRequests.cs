using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Opportunities;

public class SaveOpportunityRequest
{
    public string Title { get; set; } = string.Empty;
    public OpportunityType Type { get; set; }
    public int DepartmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Eligibility { get; set; }
    public List<int> AllowedDepartmentIds { get; set; } = [];   // empty = any department
    public List<int> AllowedGradeIds { get; set; } = [];        // empty = any grade
    public DateTime ClosingAt { get; set; }                     // UTC
}

public class ApplyRequest
{
    public string SupportingStatement { get; set; } = string.Empty;
    public IFormFile? Cv { get; set; }                          // optional, PDF only, 5 MB
}

public class UpdateApplicationStatusRequest
{
    public ApplicationStatus Status { get; set; }               // Shortlisted, Selected or Rejected
    public string? Comment { get; set; }
}

public class OpportunityQuery : PagedRequest
{
    public OpportunityStatus? Status { get; set; }              // HR only; staff always see Published
    public OpportunityType? Type { get; set; }
    public bool? OpenOnly { get; set; }
}

public class ApplicationQuery : PagedRequest
{
    public ApplicationStatus? Status { get; set; }
}
