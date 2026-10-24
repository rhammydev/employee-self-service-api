using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Opportunities;

public class OpportunitySummaryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public OpportunityType Type { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public OpportunityStatus Status { get; set; }
    public DateTime ClosingAt { get; set; }
    public bool IsOpen { get; set; }                   
    public DateTime? PublishedAt { get; set; }
}

public class OpportunityDetailResponse : OpportunitySummaryResponse
{
    public int DepartmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Eligibility { get; set; }
    public List<IdNameResponse> AllowedDepartments { get; set; } = [];
    public List<IdNameResponse> AllowedGrades { get; set; } = [];
    public bool? IsEligible { get; set; }               
    public int? MyApplicationId { get; set; }
}

public class IdNameResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ApplicationSummaryResponse
{
    public int Id { get; set; }
    public int OpportunityId { get; set; }
    public string OpportunityTitle { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ApplicationDetailResponse : ApplicationSummaryResponse
{
    public string SupportingStatement { get; set; } = string.Empty;
    public string DepartmentAtApply { get; set; } = string.Empty;
    public string GradeAtApply { get; set; } = string.Empty;
    public DateTime EligibilityCheckedAt { get; set; }
    public List<AttachmentResponse> Attachments { get; set; } = [];
    public List<ApplicationHistoryResponse> History { get; set; } = [];
}

public class ApplicationHistoryResponse
{
    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
