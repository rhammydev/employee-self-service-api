using System.Globalization;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Training;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;
using static EmployeeSelfServiceAPI.Mappings.TrainingMappings;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class TrainingApprovalService(
    ITrainingRequestRepository trainingRequestRepository,
    IEstacodeRateRepository estacodeRateRepository,
    IEmployeeRepository employeeRepository,
    INotificationService notificationService,
    IPdfService pdfService,
    IUnitOfWork unitOfWork,
    IAppClock clock) : ITrainingApprovalService
{
    private const string NotFoundMessage = "Training request not found.";

    // HR's queue: new requests to confirm, and allowances Finance sent back
    private static readonly TrainingStatus[] HrQueueStatuses =
        [TrainingStatus.PendingHRApproval, TrainingStatus.ReturnedToHR];

    private static readonly TrainingStatus[] FinanceQueueStatuses = [TrainingStatus.PendingFinanceApproval];

    public async Task<ApiResponse> GetPendingHrAsync(PagedRequest request) =>
        ApiResponse.Paged("Requests awaiting HR retrieved.",
            ToSummaries(await trainingRequestRepository.GetPagedByStatusesAsync(HrQueueStatuses, request)));

    public async Task<ApiResponse> GetPendingFinanceAsync(PagedRequest request) =>
        ApiResponse.Paged("Requests awaiting Finance retrieved.",
            ToSummaries(await trainingRequestRepository.GetPagedByStatusesAsync(FinanceQueueStatuses, request)));

    public async Task<ApiResponse> HrApproveAsync(int hrId, int id)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        if (training == null)
            return ApiResponse.NotFound(NotFoundMessage);

        if (training.EmployeeId == hrId)
            return ApiResponse.Forbidden("You cannot approve your own training request.");
        if (training.Status != TrainingStatus.PendingHRApproval)
            return ApiResponse.Conflict("This request is not awaiting HR approval.");

        var employee = training.Employee!;
        var grade = employee.Grade!;
        var rate = await estacodeRateRepository.GetActiveRateAsync(training.CountryCode, employee.GradeId);
        if (rate == null)
            return ApiResponse.Conflict(
                $"No estacode rate is configured for country {training.CountryCode} and grade {grade.Code}. " +
                "Add the rate before approving this request.");

        var now = clock.UtcNow;
        var eligibleDays = AllowanceCalculator.EligibleDays(training.DepartureDate, training.ReturnDate);

        var allowance = new EstacodeAllowance
        {
            VersionNumber = 1,
            GradeCode = grade.Code,
            CountryCode = training.CountryCode,
            DailyRate = rate.DailyRate,
            Currency = rate.Currency,
            ComputedEligibleDays = eligibleDays,
            EligibleDays = eligibleDays,
            Amount = AllowanceCalculator.Amount(eligibleDays, rate.DailyRate),
            CreatedById = hrId,
            CreatedAt = now
        };
        training.Allowances.Add(allowance);

        var from = training.Status;
        training.Status = TrainingStatus.PendingFinanceApproval;
        training.UpdatedAt = now;
        AddHistory(training, TrainingAction.HRApproved, from, hrId,
            $"Allowance v1: {eligibleDays} days x {Money(allowance.Currency, allowance.DailyRate)} = {Money(allowance.Currency, allowance.Amount)}");

        await notificationService.NotifyAsync(employee, "Training request confirmed by HR",
            $"{training.Reference} was confirmed by HR. Your provisional allowance is {Money(allowance.Currency, allowance.Amount)}, pending Finance approval.",
            relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);
        await NotifyRoleAsync(UserRole.Finance, training, "Allowance awaiting Finance approval",
            $"{training.Reference} ({training.Title}) has an allowance of {Money(allowance.Currency, allowance.Amount)} awaiting your approval.");

        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Request confirmed. Allowance version 1 generated and sent to Finance.",
            ToAllowance(allowance, isCurrent: true));
    }

    public async Task<ApiResponse> CorrectAllowanceAsync(int hrId, int id, AllowanceCorrectionRequest request)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        if (training == null)
            return ApiResponse.NotFound(NotFoundMessage);
        if (training.EmployeeId == hrId)
            return ApiResponse.Forbidden("You cannot correct the allowance on your own training request.");
        if (training.Status != TrainingStatus.ReturnedToHR)
            return ApiResponse.Conflict("Only allowances returned by Finance can be corrected.");

        var previous = training.Allowances.MaxBy(a => a.VersionNumber);
        if (previous == null)
            return ApiResponse.Conflict("This request has no allowance to correct.");

        var eligibleDays = request.EligibleDays ?? previous.EligibleDays;
        if (eligibleDays > previous.ComputedEligibleDays)
        {
            var message = $"Eligible days cannot exceed the computed {previous.ComputedEligibleDays} days.";
            return ApiResponse.Fail(message, 400, ResponseCodes.BadRequest, [new ApiError("eligibleDays", message)]);
        }

        var dailyRate = Math.Round(request.DailyRate ?? previous.DailyRate, 2, MidpointRounding.AwayFromZero);
        var now = clock.UtcNow;

        var allowance = new EstacodeAllowance
        {
            VersionNumber = previous.VersionNumber + 1,
            GradeCode = previous.GradeCode,
            CountryCode = previous.CountryCode,
            Currency = previous.Currency,
            ComputedEligibleDays = previous.ComputedEligibleDays,
            DailyRate = dailyRate,
            EligibleDays = eligibleDays,
            Amount = AllowanceCalculator.Amount(eligibleDays, dailyRate),
            IsRateOverridden = request.DailyRate.HasValue || previous.IsRateOverridden,
            CorrectionReason = request.Reason.Trim(),
            CreatedById = hrId,
            CreatedAt = now
        };
        training.Allowances.Add(allowance);

        var from = training.Status;
        training.Status = TrainingStatus.PendingFinanceApproval;
        training.UpdatedAt = now;
        AddHistory(training, TrainingAction.AllowanceCorrected, from, hrId,
            $"v{allowance.VersionNumber}: {eligibleDays} days x {Money(allowance.Currency, dailyRate)} = {Money(allowance.Currency, allowance.Amount)}. Reason: {allowance.CorrectionReason}");

        await NotifyRoleAsync(UserRole.Finance, training, "Corrected allowance awaiting Finance approval",
            $"HR submitted allowance version {allowance.VersionNumber} for {training.Reference}: {Money(allowance.Currency, allowance.Amount)}.");

        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success($"Allowance version {allowance.VersionNumber} created and sent to Finance.",
            ToAllowance(allowance, isCurrent: true));
    }

    public async Task<ApiResponse> FinanceDecisionAsync(int financeId, int id, FinanceDecisionRequest request)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        if (training == null)
            return ApiResponse.NotFound(NotFoundMessage);
        if (training.EmployeeId == financeId)
            return ApiResponse.Forbidden("You cannot decide on your own training request.");

        if (training.Status != TrainingStatus.PendingFinanceApproval)
            return ApiResponse.Conflict("This request is not awaiting Finance approval.");

        var current = training.Allowances.MaxBy(a => a.VersionNumber);
        if (current == null)
            return ApiResponse.Conflict("This request has no allowance to review.");
        if (request.AllowanceVersion != current.VersionNumber)
            return ApiResponse.Conflict(
                $"You reviewed version {request.AllowanceVersion}, but the current version is {current.VersionNumber}. Review the current version and try again.");

        var now = clock.UtcNow;
        var from = training.Status;
        var reason = request.Reason?.Trim();

        switch (request.Decision)
        {
            case FinanceDecision.Approve:
                current.ApprovedById = financeId;
                current.ApprovedAt = now;
                training.Status = TrainingStatus.Approved;
                AddHistory(training, TrainingAction.FinanceApproved, from, financeId, reason);
                await notificationService.NotifyAsync(training.Employee!, "Training allowance approved",
                    $"Finance approved {training.Reference}. Your allowance of {Money(current.Currency, current.Amount)} is confirmed and the statement PDF is available.",
                    relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);
                break;

            case FinanceDecision.Reject:
                training.Status = TrainingStatus.Rejected;
                AddHistory(training, TrainingAction.FinanceRejected, from, financeId, reason);
                await notificationService.NotifyAsync(training.Employee!, "Training request rejected by Finance",
                    $"Finance rejected {training.Reference}. Reason: {reason}",
                    relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);
                break;

            case FinanceDecision.ReturnToHR:
                training.Status = TrainingStatus.ReturnedToHR;
                AddHistory(training, TrainingAction.FinanceReturnedToHR, from, financeId, reason);
                await NotifyRoleAsync(UserRole.HR, training, "Allowance returned by Finance",
                    $"Finance returned allowance version {current.VersionNumber} for {training.Reference}. Reason: {reason}");
                break;
        }

        training.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(request.Decision switch
        {
            FinanceDecision.Approve => "Allowance approved. The statement PDF is now available.",
            FinanceDecision.Reject => "Request rejected.",
            _ => "Allowance returned to HR for correction."
        });
    }

    public async Task<(ApiResponse? Error, FileDownload? File)> GetAllowancePdfAsync(int userId, IReadOnlyList<UserRole> roles, int id)
    {
        var training = await trainingRequestRepository.GetDetailsAsync(id);
        if (training == null)
            return (ApiResponse.NotFound(NotFoundMessage), null);
        if (!TrainingAccess.CanView(training, userId, roles))
            return (ApiResponse.Forbidden("You do not have access to this training request."), null);

        var approved = training.Allowances.Where(a => a.ApprovedAt != null).MaxBy(a => a.VersionNumber);
        if (approved == null)
            return (ApiResponse.Conflict("The allowance statement is available only after Finance approval."), null);

        var pdf = pdfService.GenerateAllowanceStatement(training, approved, clock.UtcNow);
        return (null, new FileDownload(new MemoryStream(pdf), "application/pdf",
            $"{training.Reference}-allowance-v{approved.VersionNumber}.pdf"));
    }

    // ---- helpers ----

    private async Task NotifyRoleAsync(UserRole role, TrainingRequest training, string title, string message)
    {
        foreach (var recipient in await employeeRepository.GetActiveByRoleAsync(role))
        {
            if (recipient.Id == training.EmployeeId) continue;   
            await notificationService.NotifyAsync(recipient, title, message,
                relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);
        }
    }

    private void AddHistory(TrainingRequest training, TrainingAction action, TrainingStatus? from, int actorId, string? comment = null) =>
        training.StatusHistory.Add(new TrainingStatusHistory
        {
            Action = action,
            FromStatus = from,
            ToStatus = training.Status,
            ActorId = actorId,
            Comment = comment,
            CreatedAt = clock.UtcNow
        });

    private static string Money(string currency, decimal amount) =>
        $"{currency} {amount.ToString("N2", CultureInfo.InvariantCulture)}";
}
