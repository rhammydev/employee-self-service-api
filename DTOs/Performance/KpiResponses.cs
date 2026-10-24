using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Performance;

public class ReviewPeriodResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly SubmissionDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class KpiPlanSummaryResponse
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int ReviewPeriodId { get; set; }
    public string ReviewPeriodName { get; set; } = string.Empty;
    public KpiPlanStatus Status { get; set; }
    public string? AssignedReviewerName { get; set; }
    public DateOnly OriginalDeadline { get; set; }
    public DateOnly EffectiveDeadline { get; set; }
    public bool IsPastDeadline { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal? OverallScore { get; set; }
}

public class KpiPlanDetailResponse
{
    public KpiPlanSummaryResponse Summary { get; set; } = new();
    public int? AssignedReviewerId { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public List<KpiItemResponse> Items { get; set; } = [];
    public List<KpiReopeningResponse> Reopenings { get; set; } = [];
    public List<KpiHistoryResponse> History { get; set; } = [];
}

public class KpiItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Target { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int SortOrder { get; set; }
    public decimal? ActualValue { get; set; }
    public string? EvidenceComment { get; set; }
    public decimal? AchievementPercent { get; set; }
    public decimal? WeightedScore { get; set; }
    public List<AttachmentResponse> Evidence { get; set; } = [];
}

public class KpiReopeningResponse
{
    public DateOnly PreviousDeadline { get; set; }
    public DateOnly NewDeadline { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReopenedByName { get; set; } = string.Empty;
    public DateTime ReopenedAt { get; set; }
}

public class KpiHistoryResponse
{
    public KpiAction Action { get; set; }
    public KpiPlanStatus? FromStatus { get; set; }
    public KpiPlanStatus ToStatus { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
