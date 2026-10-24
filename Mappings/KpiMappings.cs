using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Performance;

namespace EmployeeSelfServiceAPI.Mappings;

public static class KpiMappings
{
    public static ReviewPeriodResponse ToPeriod(ReviewPeriod p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        SubmissionDeadline = p.SubmissionDeadline,
        CreatedAt = p.CreatedAt
    };

    public static KpiPlanSummaryResponse ToSummary(KpiPlan p, DateOnly today) => new()
    {
        Id = p.Id,
        EmployeeId = p.EmployeeId,
        EmployeeName = Name(p.Employee),
        ReviewPeriodId = p.ReviewPeriodId,
        ReviewPeriodName = p.ReviewPeriod?.Name ?? string.Empty,
        Status = p.Status,
        AssignedReviewerName = p.AssignedReviewer == null ? null : Name(p.AssignedReviewer),
        OriginalDeadline = p.ReviewPeriod?.SubmissionDeadline ?? p.EffectiveDeadline,
        EffectiveDeadline = p.EffectiveDeadline,
        IsPastDeadline = today > p.EffectiveDeadline,
        TotalWeight = p.Items.Sum(i => i.Weight),
        OverallScore = p.OverallScore
    };

    public static KpiPlanDetailResponse ToDetail(KpiPlan p, DateOnly today, ILookup<int, Attachment> evidenceByItem) => new()
    {
        Summary = ToSummary(p, today),
        AssignedReviewerId = p.AssignedReviewerId,
        ActivatedAt = p.ActivatedAt,
        SubmittedAt = p.SubmittedAt,
        FinalizedAt = p.FinalizedAt,
        Items = p.Items.OrderBy(i => i.SortOrder).Select(i => new KpiItemResponse
        {
            Id = i.Id,
            Title = i.Title,
            Description = i.Description,
            Target = i.Target,
            Unit = i.Unit,
            Weight = i.Weight,
            SortOrder = i.SortOrder,
            ActualValue = i.ActualValue,
            EvidenceComment = i.EvidenceComment,
            AchievementPercent = Round2(i.AchievementPercent),
            WeightedScore = Round2(i.WeightedScore),
            Evidence = evidenceByItem[i.Id].Select(a => new AttachmentResponse
            {
                Id = a.Id,
                Kind = a.ParentType,
                FileName = a.OriginalFileName,
                ContentType = a.ContentType,
                SizeBytes = a.SizeBytes,
                UploadedAt = a.UploadedAt
            }).ToList()
        }).ToList(),
        Reopenings = p.Reopenings.OrderBy(r => r.ReopenedAt).Select(r => new KpiReopeningResponse
        {
            PreviousDeadline = r.PreviousDeadline,
            NewDeadline = r.NewDeadline,
            Reason = r.Reason,
            ReopenedByName = Name(r.ReopenedBy),
            ReopenedAt = r.ReopenedAt
        }).ToList(),
        History = p.StatusHistory.OrderBy(h => h.CreatedAt).Select(h => new KpiHistoryResponse
        {
            Action = h.Action,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ActorName = Name(h.Actor),
            Comment = h.Comment,
            CreatedAt = h.CreatedAt
        }).ToList()
    };

    public static PagedResult<KpiPlanSummaryResponse> ToSummaries(PagedResult<KpiPlan> page, DateOnly today) => new()
    {
        Pagination = page.Pagination,
        Items = page.Items.Select(p => ToSummary(p, today)).ToList()
    };

    private static string Name(Employee? e) => e == null ? string.Empty : $"{e.FirstName} {e.LastName}";

    // Scores are stored with extra precision and shown to 2 decimal places
    private static decimal? Round2(decimal? value) =>
        value.HasValue ? Math.Round(value.Value, 2, MidpointRounding.AwayFromZero) : null;
}
