using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Training;

namespace EmployeeSelfServiceAPI.Mappings;

// Shared by TrainingRequestService and TrainingApprovalService
public static class TrainingMappings
{
    public static string FullName(Employee e) => $"{e.FirstName} {e.LastName}";

    public static PagedResult<TrainingRequestSummaryResponse> ToSummaries(PagedResult<TrainingRequest> page) => new()
    {
        Pagination = page.Pagination,
        Items = page.Items.Select(t => new TrainingRequestSummaryResponse
        {
            Id = t.Id,
            Reference = t.Reference,
            Title = t.Title,
            EmployeeName = t.Employee == null ? string.Empty : FullName(t.Employee),
            CountryCode = t.CountryCode,
            City = t.City,
            TrainingStartDate = t.TrainingStartDate,
            TrainingEndDate = t.TrainingEndDate,
            Status = t.Status,
            AssignedReviewerName = t.AssignedReviewer == null ? null : FullName(t.AssignedReviewer),
            SubmittedAt = t.SubmittedAt,
            CreatedAt = t.CreatedAt
        }).ToList()
    };

    public static TrainingRequestDetailResponse ToDetail(TrainingRequest t, List<Attachment> attachments)
    {
        var currentVersion = t.Allowances.Count == 0 ? 0 : t.Allowances.Max(a => a.VersionNumber);

        return new TrainingRequestDetailResponse
        {
            Id = t.Id,
            Reference = t.Reference,
            EmployeeId = t.EmployeeId,
            EmployeeName = t.Employee == null ? string.Empty : FullName(t.Employee),
            Title = t.Title,
            Provider = t.Provider,
            CountryCode = t.CountryCode,
            City = t.City,
            TrainingStartDate = t.TrainingStartDate,
            TrainingEndDate = t.TrainingEndDate,
            DepartureDate = t.DepartureDate,
            ReturnDate = t.ReturnDate,
            BusinessJustification = t.BusinessJustification,
            CourseFee = t.CourseFee,
            CourseFeeCurrency = t.CourseFeeCurrency,
            FlightEstimate = t.FlightEstimate,
            FlightEstimateCurrency = t.FlightEstimateCurrency,
            Status = t.Status,
            AssignedReviewerId = t.AssignedReviewerId,
            AssignedReviewerName = t.AssignedReviewer == null ? null : FullName(t.AssignedReviewer),
            SubmittedAt = t.SubmittedAt,
            CreatedAt = t.CreatedAt,
            Attachments = attachments.Select(a => new AttachmentResponse
            {
                Id = a.Id,
                Kind = a.ParentType,
                FileName = a.OriginalFileName,
                ContentType = a.ContentType,
                SizeBytes = a.SizeBytes,
                UploadedAt = a.UploadedAt
            }).ToList(),
            History = t.StatusHistory.OrderBy(h => h.CreatedAt).Select(h => new TrainingHistoryResponse
            {
                Action = h.Action,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                ActorName = h.Actor == null ? string.Empty : FullName(h.Actor),
                Comment = h.Comment,
                CreatedAt = h.CreatedAt
            }).ToList(),
            Allowances = t.Allowances
                .OrderBy(a => a.VersionNumber)
                .Select(a => ToAllowance(a, a.VersionNumber == currentVersion))
                .ToList()
        };
    }

    public static AllowanceResponse ToAllowance(EstacodeAllowance a, bool isCurrent) => new()
    {
        VersionNumber = a.VersionNumber,
        IsCurrent = isCurrent,
        GradeCode = a.GradeCode,
        CountryCode = a.CountryCode,
        DailyRate = a.DailyRate,
        Currency = a.Currency,
        ComputedEligibleDays = a.ComputedEligibleDays,
        EligibleDays = a.EligibleDays,
        Amount = a.Amount,
        IsRateOverridden = a.IsRateOverridden,
        CorrectionReason = a.CorrectionReason,
        CreatedByName = a.CreatedBy == null ? null : FullName(a.CreatedBy),
        CreatedAt = a.CreatedAt,
        ApprovedByName = a.ApprovedBy == null ? null : FullName(a.ApprovedBy),
        ApprovedAt = a.ApprovedAt
    };
}
