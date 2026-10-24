using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Performance;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;
using static EmployeeSelfServiceAPI.Mappings.KpiMappings;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class KpiPlanService(
    IKpiRepository kpiRepository,
    IEmployeeRepository employeeRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IAppClock clock) : IKpiPlanService
{
    private const string PlanNotFound = "KPI plan not found.";

    public async Task<ApiResponse> CreatePeriodAsync(int hrId, CreateReviewPeriodRequest request)
    {
        var period = new ReviewPeriod
        {
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SubmissionDeadline = request.SubmissionDeadline,
            CreatedById = hrId,
            CreatedAt = clock.UtcNow
        };
        await kpiRepository.AddPeriodAsync(period);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Review period created.", ToPeriod(period), 201);
    }

    public async Task<ApiResponse> GetPeriodsAsync(PagedRequest request)
    {
        var page = await kpiRepository.GetPagedPeriodsAsync(request);
        return ApiResponse.Paged("Review periods retrieved.", new PagedResult<ReviewPeriodResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(ToPeriod).ToList()
        });
    }

    public async Task<ApiResponse> CreatePlanAsync(int hrId, CreateKpiPlanRequest request)
    {
        if (request.EmployeeId == hrId)
            return ApiResponse.Forbidden("You cannot create a KPI plan for yourself. Another HR user must.");

        var period = await kpiRepository.GetPeriodAsync(request.ReviewPeriodId);
        if (period == null)
            return ApiResponse.Fail("Review period does not exist.", 400, ResponseCodes.BadRequest,
                [new ApiError("reviewPeriodId", "Review period does not exist.")]);

        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId);
        if (employee is not { IsActive: true })
            return ApiResponse.Fail("Employee does not exist or is not active.", 400, ResponseCodes.BadRequest,
                [new ApiError("employeeId", "Employee does not exist or is not active.")]);

        if (await kpiRepository.PlanExistsAsync(request.EmployeeId, request.ReviewPeriodId))
            return ApiResponse.Conflict("This employee already has a KPI plan for this review period.");

        var now = clock.UtcNow;
        var plan = new KpiPlan
        {
            EmployeeId = request.EmployeeId,
            ReviewPeriodId = request.ReviewPeriodId,
            Status = KpiPlanStatus.Draft,
            EffectiveDeadline = period.SubmissionDeadline,
            CreatedById = hrId,
            CreatedAt = now,
            UpdatedAt = now
        };
        AddHistory(plan, KpiAction.Created, null, hrId);
        await kpiRepository.AddPlanAsync(plan);
        await unitOfWork.SaveChangesAsync();   

        return ApiResponse.Success("KPI plan created as draft.", new { plan.Id, plan.Status }, 201);
    }

    public async Task<ApiResponse> GetAllAsync(KpiPlanQuery query) =>
        ApiResponse.Paged("KPI plans retrieved.", ToSummaries(await kpiRepository.GetPagedPlansAsync(query), clock.Today));


    public async Task<ApiResponse> AddItemAsync(int hrId, int planId, SaveKpiItemRequest request)
    {
        var (plan, error) = await LoadDraftForHrAsync(hrId, planId);
        if (error != null) return error;

        var item = new KpiItem { SortOrder = plan!.Items.Count == 0 ? 1 : plan.Items.Max(i => i.SortOrder) + 1 };
        Apply(item, request);
        plan.Items.Add(item);
        plan.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("KPI added.", new { item.Id, TotalWeight = plan.Items.Sum(i => i.Weight) }, 201);
    }

    public async Task<ApiResponse> UpdateItemAsync(int hrId, int planId, int itemId, SaveKpiItemRequest request)
    {
        var (plan, error) = await LoadDraftForHrAsync(hrId, planId);
        if (error != null) return error;

        var item = plan!.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            return ApiResponse.NotFound("KPI not found in this plan.");

        Apply(item, request);
        plan.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("KPI updated.", new { item.Id, TotalWeight = plan.Items.Sum(i => i.Weight) });
    }

    public async Task<ApiResponse> DeleteItemAsync(int hrId, int planId, int itemId)
    {
        var (plan, error) = await LoadDraftForHrAsync(hrId, planId);
        if (error != null) return error;

        var item = plan!.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            return ApiResponse.NotFound("KPI not found in this plan.");

        kpiRepository.RemoveItem(item);
        plan.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("KPI removed.");
    }

    public async Task<ApiResponse> ActivateAsync(int hrId, int planId, ActivateKpiPlanRequest request)
    {
        var (plan, error) = await LoadDraftForHrAsync(hrId, planId);
        if (error != null) return error;

        // KPI-01.5: weights total exactly 100.00, no tolerance
        var totalWeight = plan!.Items.Sum(i => i.Weight);
        if (plan.Items.Count == 0 || totalWeight != KpiCalculator.RequiredWeightTotal)
            return ApiResponse.Fail($"KPI weights must total exactly 100.00 (currently {totalWeight:0.00}).", 400);

        if (plan.EffectiveDeadline < clock.Today)
            return ApiResponse.Fail("The review period's submission deadline has already passed.", 400);

        // Decision: HR may pick the reviewer; otherwise the employee's reporting manager at this moment
        var reviewerId = request.ReviewerId ?? plan.Employee!.ReportingManagerId;
        if (reviewerId == null)
            return ApiResponse.Fail("The employee has no reporting manager. Choose a reviewer.", 400);
        if (reviewerId == plan.EmployeeId)
            return ApiResponse.Fail("The plan owner cannot be their own reviewer.", 400);

        var reviewer = await employeeRepository.GetByIdWithRolesAsync(reviewerId.Value);
        if (reviewer is not { IsActive: true } || reviewer.Roles.All(r => r.Role != UserRole.Manager))
            return ApiResponse.Fail("The reviewer must be an active Manager.", 400);

        var now = clock.UtcNow;
        var from = plan.Status;
        plan.Status = KpiPlanStatus.Active;
        plan.AssignedReviewerId = reviewer.Id;
        plan.ActivatedById = hrId;
        plan.ActivatedAt = now;
        plan.UpdatedAt = now;
        AddHistory(plan, KpiAction.Activated, from, hrId, $"Reviewer: {reviewer.FirstName} {reviewer.LastName}");

        await notificationService.NotifyAsync(plan.Employee!, "New KPI plan assigned",
            $"Your KPI plan for {plan.ReviewPeriod!.Name} is active. Record your results by {plan.EffectiveDeadline:dd MMM yyyy}.",
            relatedEntity: nameof(KpiPlan), relatedEntityId: plan.Id);
        await notificationService.NotifyAsync(reviewer, "You are reviewing a KPI plan",
            $"You are the reviewer for {plan.Employee!.FirstName} {plan.Employee.LastName}'s KPI plan for {plan.ReviewPeriod.Name}.",
            relatedEntity: nameof(KpiPlan), relatedEntityId: plan.Id);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("KPI plan activated.");
    }

    public async Task<ApiResponse> ReopenAsync(int hrId, int planId, ReopenKpiPlanRequest request)
    {
        var plan = await kpiRepository.GetPlanForUpdateAsync(planId);
        if (plan == null)
            return ApiResponse.NotFound(PlanNotFound);
        if (plan.EmployeeId == hrId)
            return ApiResponse.Forbidden("You cannot reopen your own KPI plan.");

        // KPI-03.4
        switch (plan.Status)
        {
            case KpiPlanStatus.Finalized:
                return ApiResponse.Conflict("A finalized KPI plan cannot be reopened.");
            case KpiPlanStatus.Submitted:
                return ApiResponse.Conflict("A submitted plan must be returned by the manager before it can be reopened.");
            case KpiPlanStatus.Draft:
                return ApiResponse.Conflict("A draft plan has no deadline to reopen. Activate it instead.");
        }

        if (request.NewDeadline <= clock.Today)
            return ApiResponse.Fail("The new deadline must be in the future.", 400, ResponseCodes.BadRequest,
                [new ApiError("newDeadline", "The new deadline must be in the future.")]);

        var now = clock.UtcNow;
        plan.Reopenings.Add(new KpiDeadlineReopening
        {
            PreviousDeadline = plan.EffectiveDeadline,
            NewDeadline = request.NewDeadline,
            Reason = request.Reason.Trim(),
            ReopenedById = hrId,
            ReopenedAt = now
        });
        var previous = plan.EffectiveDeadline;
        plan.EffectiveDeadline = request.NewDeadline;  
        plan.UpdatedAt = now;
        AddHistory(plan, KpiAction.Reopened, plan.Status, hrId,
            $"Deadline {previous:yyyy-MM-dd} -> {request.NewDeadline:yyyy-MM-dd}. {request.Reason.Trim()}");

        await notificationService.NotifyAsync(plan.Employee!, "KPI plan reopened",
            $"HR extended your KPI deadline for {plan.ReviewPeriod!.Name} to {request.NewDeadline:dd MMM yyyy}.",
            relatedEntity: nameof(KpiPlan), relatedEntityId: plan.Id);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success($"Plan reopened until {request.NewDeadline:yyyy-MM-dd}.");
    }

    // ---- helpers ----

    private async Task<(KpiPlan? Plan, ApiResponse? Error)> LoadDraftForHrAsync(int hrId, int planId)
    {
        var plan = await kpiRepository.GetPlanForUpdateAsync(planId);
        if (plan == null)
            return (null, ApiResponse.NotFound(PlanNotFound));
        if (plan.EmployeeId == hrId)
            return (null, ApiResponse.Forbidden("You cannot edit your own KPI plan. Another HR user must."));
        if (plan.Status != KpiPlanStatus.Draft)
            return (null, ApiResponse.Conflict("Targets and weights can only be changed while the plan is Draft."));
        return (plan, null);
    }

    private static void Apply(KpiItem item, SaveKpiItemRequest request)
    {
        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim();
        item.Target = request.Target;
        item.Unit = request.Unit.Trim();
        item.Weight = request.Weight;
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
