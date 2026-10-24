using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Performance;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;
using static EmployeeSelfServiceAPI.Mappings.KpiMappings;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class KpiProgressService(
    IKpiRepository kpiRepository,
    IAttachmentRepository attachmentRepository,
    IFileStorageService fileStorageService,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IAppClock clock) : IKpiProgressService
{
    private const string PlanNotFound = "KPI plan not found.";

    public async Task<ApiResponse> GetMineAsync(int employeeId, KpiPlanQuery query) =>
        ApiResponse.Paged("KPI plans retrieved.",
            ToSummaries(await kpiRepository.GetPagedPlansForEmployeeAsync(employeeId, query), clock.Today));

    public async Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int planId)
    {
        var plan = await kpiRepository.GetPlanDetailsAsync(planId);
        if (plan == null)
            return ApiResponse.NotFound(PlanNotFound);
        if (!KpiAccess.CanView(plan, userId, roles))
            return ApiResponse.Forbidden("You do not have access to this KPI plan.");

        var evidence = await attachmentRepository.GetByParentIdsAsync(AttachmentParentType.KpiItem,
            plan.Items.Select(i => i.Id).ToList());

        return ApiResponse.Success("KPI plan retrieved.",
            ToDetail(plan, clock.Today, evidence.ToLookup(a => a.ParentId)));
    }

    public async Task<ApiResponse> UpdateProgressAsync(int employeeId, int planId, int itemId, UpdateKpiProgressRequest request)
    {
        var plan = await kpiRepository.GetPlanForUpdateAsync(planId);
        var error = CheckOwnerCanEdit(plan, employeeId);
        if (error != null) return error;

        var item = plan!.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            return ApiResponse.NotFound("KPI not found in this plan.");

        // KPI-02.4: staff change only actuals and evidence comments, never targets, weights or scores
        item.ActualValue = request.ActualValue;
        item.EvidenceComment = request.EvidenceComment?.Trim();
        plan.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Progress saved.");
    }

    public async Task<ApiResponse> UploadEvidenceAsync(int employeeId, int planId, int itemId, IFormFile file)
    {
        var plan = await kpiRepository.GetPlanForUpdateAsync(planId);
        var error = CheckOwnerCanEdit(plan, employeeId);
        if (error != null) return error;

        if (plan!.Items.All(i => i.Id != itemId))
            return ApiResponse.NotFound("KPI not found in this plan.");

        // PDF, JPG or PNG up to 5 MB; downloadable only via /attachments/{id}/download
        var (fileType, fileError) = await fileStorageService.ValidateAsync(file, FileTypes.Documents);
        if (fileError != null)
            return ApiResponse.Fail(fileError, 400, ResponseCodes.BadRequest, [new ApiError("file", fileError)]);

        var attachment = new Attachment
        {
            ParentType = AttachmentParentType.KpiItem,
            ParentId = itemId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = await fileStorageService.SaveAsync(file, fileType!),
            ContentType = fileType!.ContentType,
            SizeBytes = file.Length,
            UploadedById = employeeId,
            UploadedAt = clock.UtcNow
        };
        await attachmentRepository.AddAsync(attachment);
        plan.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Evidence uploaded.", new { attachment.Id, FileName = attachment.OriginalFileName }, 201);
    }

    public async Task<ApiResponse> SubmitAsync(int employeeId, int planId)
    {
        var plan = await kpiRepository.GetPlanForUpdateAsync(planId);
        var error = CheckOwnerCanEdit(plan, employeeId);
        if (error != null) return error;

        var missing = plan!.Items.Where(i => i.ActualValue == null).Select(i => i.Title).ToList();
        if (missing.Count > 0)
            return ApiResponse.Fail($"Record an actual value for every KPI before submitting. Missing: {string.Join(", ", missing)}.", 400);

        // KPI-02.3: submitted plans are locked until the manager returns them
        var from = plan.Status;
        plan.Status = KpiPlanStatus.Submitted;
        plan.SubmittedAt = clock.UtcNow;
        plan.UpdatedAt = clock.UtcNow;
        AddHistory(plan, KpiAction.Submitted, from, employeeId);

        await notificationService.NotifyAsync(plan.AssignedReviewer!, "KPI plan awaiting your review",
            $"{plan.Employee!.FirstName} {plan.Employee.LastName} submitted their KPI plan for {plan.ReviewPeriod!.Name}.",
            relatedEntity: nameof(KpiPlan), relatedEntityId: plan.Id);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("KPI plan submitted for review.");
    }

    public async Task<ApiResponse> GetPendingReviewAsync(int managerId, PagedRequest request) =>
        ApiResponse.Paged("Plans awaiting your review retrieved.",
            ToSummaries(await kpiRepository.GetPagedPendingReviewAsync(managerId, request), clock.Today));

    public async Task<ApiResponse> ReviewAsync(int managerId, int planId, KpiReviewRequest request)
    {
        var plan = await kpiRepository.GetPlanForUpdateAsync(planId);
        if (plan == null)
            return ApiResponse.NotFound(PlanNotFound);

        if (plan.AssignedReviewerId != managerId || plan.EmployeeId == managerId)
            return ApiResponse.Forbidden("This KPI plan is not assigned to you for review.");

        if (plan.Status != KpiPlanStatus.Submitted)
            return ApiResponse.Conflict("This KPI plan is not awaiting review.");

        var now = clock.UtcNow;
        var from = plan.Status;
        var comment = request.Comment?.Trim();

        if (request.Approve)
        {
            foreach (var item in plan.Items)
            {
                var achievement = KpiCalculator.AchievementPercent(item.ActualValue ?? 0, item.Target);
                item.AchievementPercent = Math.Round(achievement, 4, MidpointRounding.AwayFromZero);
                item.WeightedScore = Math.Round(KpiCalculator.WeightedScore(achievement, item.Weight), 4, MidpointRounding.AwayFromZero);
            }
            plan.OverallScore = KpiCalculator.OverallScore(plan.Items.Select(i =>
                KpiCalculator.WeightedScore(KpiCalculator.AchievementPercent(i.ActualValue ?? 0, i.Target), i.Weight)));
            plan.Status = KpiPlanStatus.Finalized;
            plan.FinalizedAt = now;
            AddHistory(plan, KpiAction.Finalized, from, managerId, comment);
        }
        else
        {
            plan.Status = KpiPlanStatus.Returned;
            AddHistory(plan, KpiAction.Returned, from, managerId, comment);
        }
        plan.UpdatedAt = now;

        await notificationService.NotifyAsync(plan.Employee!,
            request.Approve ? "KPI plan finalized" : "KPI plan returned",
            request.Approve
                ? $"Your KPI plan for {plan.ReviewPeriod!.Name} was finalized with an overall score of {plan.OverallScore:0.00}."
                : $"Your manager returned your KPI plan for {plan.ReviewPeriod!.Name}. Comment: {comment}",
            relatedEntity: nameof(KpiPlan), relatedEntityId: plan.Id);

        // RowVersion: two simultaneous reviews -> the second gets 409
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(request.Approve
            ? $"KPI plan finalized. Overall score {plan.OverallScore:0.00}."
            : "KPI plan returned to the employee.");
    }

    // ---- helpers ----

    private ApiResponse? CheckOwnerCanEdit(KpiPlan? plan, int employeeId)
    {
        if (plan == null)
            return ApiResponse.NotFound(PlanNotFound);
        if (plan.EmployeeId != employeeId)
            return ApiResponse.Forbidden("You can only update your own KPI plan.");
        if (plan.Status is not (KpiPlanStatus.Active or KpiPlanStatus.Returned))
            return ApiResponse.Conflict(plan.Status == KpiPlanStatus.Finalized
                ? "This KPI plan is finalized and can no longer be changed."
                : $"A plan in {plan.Status} status cannot be changed.");
        if (clock.Today > plan.EffectiveDeadline)
            return ApiResponse.Conflict($"The submission deadline ({plan.EffectiveDeadline:yyyy-MM-dd}) has passed. Ask HR to reopen the plan.");
        return null;
    }

    private void AddHistory(KpiPlan plan, KpiAction action, KpiPlanStatus? from, int actorId, string? comment = null) =>
        plan.StatusHistory.Add(new KpiPlanStatusHistory
        {
            Action = action,
            FromStatus = from,
            ToStatus = plan.Status,
            ActorId = actorId,
            Comment = comment,
            CreatedAt = clock.UtcNow
        });
}
