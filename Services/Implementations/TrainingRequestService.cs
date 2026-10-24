using System.Security.Cryptography;
using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Training;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;
using static EmployeeSelfServiceAPI.Mappings.TrainingMappings;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class TrainingRequestService(
    ITrainingRequestRepository trainingRequestRepository,
    IEmployeeRepository employeeRepository,
    IAttachmentRepository attachmentRepository,
    IFileStorageService fileStorageService,
    IAuditLogRepository auditLogRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IAppClock clock):ITrainingRequestService
{
    private const string NotFoundMessage = "Training request not found.";

    private static readonly TrainingStatus[] WithdrawableStatuses =
    [
        TrainingStatus.Draft, TrainingStatus.PendingManagerApproval, TrainingStatus.PendingHRApproval,
        TrainingStatus.PendingFinanceApproval, TrainingStatus.ReturnedToHR
    ];
    
    public async Task<ApiResponse> CreateAsync(int employeeId, SaveTrainingRequest request)
    {
        var now = clock.UtcNow;
        var training = new TrainingRequest
        {
            Reference = $"TRN-{now:yyyy}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}",
            EmployeeId = employeeId,
            Status = TrainingStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };
        Apply(training, request);
        AddHistory(training, TrainingAction.Created, null, employeeId);

        await trainingRequestRepository.AddAsync(training);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Training request saved as draft.",
            new { training.Id, training.Reference, training.Status }, 201);
    }

    public async Task<ApiResponse> UpdateAsync(int employeeId, int id, SaveTrainingRequest request)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        var error = CheckOwnedDraft(training, employeeId, "edited");
        if (error != null) return error;

        Apply(training!, request);
        training!.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Training request updated.");
    }


    public async Task<ApiResponse> UploadSupportingFileAsync(int employeeId, int id, IFormFile file)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        var error = CheckOwnedDraft(training, employeeId, "changed");
        if (error != null) return error;

        var (fileType, fileError) = await fileStorageService.ValidateAsync(file, FileTypes.Documents);
        if (fileError != null)
            return ApiResponse.Fail(fileError, 400, ResponseCodes.BadRequest, [new ApiError("file", fileError)]);

        var attachment = new Attachment
        {
            ParentType = AttachmentParentType.TrainingRequest,
            ParentId = id,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = await fileStorageService.SaveAsync(file, fileType!),
            ContentType = fileType!.ContentType,
            SizeBytes = file.Length,
            UploadedById = employeeId,
            UploadedAt = clock.UtcNow
        };
        await attachmentRepository.AddAsync(attachment);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("File uploaded.", new { attachment.Id, FileName = attachment.OriginalFileName }, 201);
    }

    public async Task<ApiResponse> SubmitAsync(int employeeId, int id)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        var error = CheckOwnedDraft(training, employeeId, "submitted");
        if (error != null) return error;

        var manager = training!.Employee?.ReportingManager;
        if (manager is not { IsActive: true })
            return ApiResponse.Fail("You have no active reporting manager. Contact HR before submitting.", 400);

        var from = training.Status;
        training.Status = TrainingStatus.PendingManagerApproval;
        training.AssignedReviewerId = manager.Id;    
        training.SubmittedAt = clock.UtcNow;
        training.UpdatedAt = clock.UtcNow;
        AddHistory(training, TrainingAction.Submitted, from, employeeId);

        await notificationService.NotifyAsync(manager, "Training request awaiting your review",
            $"{FullName(training.Employee!)} submitted {training.Reference} ({training.Title}) for your approval.",
            relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Training request submitted for manager approval.");
    }

    public async Task<ApiResponse> SubmitEvidenceAsync(int employeeId, int id, IFormFile file)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        if (training == null)
            return ApiResponse.NotFound(NotFoundMessage);
        if (training.EmployeeId != employeeId)
            return ApiResponse.Forbidden("Only the owner can submit completion evidence.");

        if (training.Status != TrainingStatus.Approved)
            return ApiResponse.Conflict("Completion evidence can only be submitted for approved requests.");
        if (clock.Today <= training.TrainingEndDate)
            return ApiResponse.Conflict(
                $"Completion evidence can be submitted after the training ends on {training.TrainingEndDate:yyyy-MM-dd}.");

        var (fileType, fileError) = await fileStorageService.ValidateAsync(file, FileTypes.Documents);
        if (fileError != null)
            return ApiResponse.Fail(fileError, 400, ResponseCodes.BadRequest, [new ApiError("file", fileError)]);

        await attachmentRepository.AddAsync(new Attachment
        {
            ParentType = AttachmentParentType.TrainingEvidence,
            ParentId = id,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = await fileStorageService.SaveAsync(file, fileType!),
            ContentType = fileType!.ContentType,
            SizeBytes = file.Length,
            UploadedById = employeeId,
            UploadedAt = clock.UtcNow
        });

        var from = training.Status;
        training.Status = TrainingStatus.EvidenceSubmitted;
        training.UpdatedAt = clock.UtcNow;
        AddHistory(training, TrainingAction.EvidenceSubmitted, from, employeeId, Path.GetFileName(file.FileName));

        foreach (var hr in await employeeRepository.GetActiveByRoleAsync(UserRole.HR))
        {
            if (hr.Id == training.EmployeeId) continue;
            await notificationService.NotifyAsync(hr, "Completion evidence awaiting review",
                $"{FullName(training.Employee!)} submitted completion evidence for {training.Reference} ({training.Title}).",
                relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);
        }

        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Completion evidence submitted for HR review.");
    }

    public async Task<ApiResponse> WithdrawAsync(int employeeId, int id, ReasonRequest request)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        if (training == null)
            return ApiResponse.NotFound(NotFoundMessage);
        if (training.EmployeeId != employeeId)
            return ApiResponse.Forbidden("Only the owner can withdraw this request.");
        if (!WithdrawableStatuses.Contains(training.Status))
            return ApiResponse.Conflict($"A request in {training.Status} status cannot be withdrawn.");

        var reviewers = await GetCurrentReviewersAsync(training); 

        var from = training.Status;
        training.Status = TrainingStatus.Withdrawn;
        training.UpdatedAt = clock.UtcNow;
        AddHistory(training, TrainingAction.Withdrawn, from, employeeId, request.Reason);  

        foreach (var reviewer in reviewers)
        {
            await notificationService.NotifyAsync(reviewer, "Training request withdrawn",
                $"{training.Reference} ({training.Title}) was withdrawn by the requester. Reason: {request.Reason}",
                relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);
        }
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Training request withdrawn.");
    }

    public async Task<ApiResponse> ManagerDecisionAsync(int managerId, int id, ManagerDecisionRequest request)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        if (training == null)
            return ApiResponse.NotFound(NotFoundMessage);

        if (training.AssignedReviewerId != managerId || training.EmployeeId == managerId)
            return ApiResponse.Forbidden("This request is not assigned to you.");

        if (training.Status != TrainingStatus.PendingManagerApproval)
            return ApiResponse.Conflict("This request is no longer awaiting manager approval.");

        var from = training.Status;
        training.Status = request.Approve ? TrainingStatus.PendingHRApproval : TrainingStatus.Rejected;
        training.UpdatedAt = clock.UtcNow;
        AddHistory(training, request.Approve ? TrainingAction.ManagerApproved : TrainingAction.ManagerRejected,
            from, managerId, request.Comment);

        await notificationService.NotifyAsync(training.Employee!,
            request.Approve ? "Training request approved by your manager" : "Training request rejected",
            request.Approve
                ? $"{training.Reference} was approved by your manager and is now with HR."
                : $"{training.Reference} was rejected by your manager. Comment: {request.Comment}",
            relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);

        if (request.Approve)
        {
            foreach (var hr in await employeeRepository.GetActiveByRoleAsync(UserRole.HR))
            {
                if (hr.Id == training.EmployeeId) continue;
                await notificationService.NotifyAsync(hr, "Training request awaiting HR review",
                    $"{training.Reference} ({training.Title}) is ready for HR confirmation.",
                    relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);
            }
        }

        await unitOfWork.SaveChangesAsync();
        return ApiResponse.Success(request.Approve ? "Request approved and sent to HR." : "Request rejected.");
    }

    public async Task<ApiResponse> ReassignReviewerAsync(int adminId, int id, ReassignReviewerRequest request)
    {
        var training = await trainingRequestRepository.GetForUpdateAsync(id);
        if (training == null)
            return ApiResponse.NotFound(NotFoundMessage);
        if (training.Status != TrainingStatus.PendingManagerApproval)
            return ApiResponse.Conflict("Only requests pending manager approval can be reassigned.");   
        if (request.NewReviewerId == training.EmployeeId)
            return ApiResponse.Fail("The requester cannot review their own request.", 400);
        if (request.NewReviewerId == training.AssignedReviewerId)
            return ApiResponse.Conflict("This manager is already the assigned reviewer.");

        var newReviewer = await employeeRepository.GetByIdWithRolesAsync(request.NewReviewerId);
        if (newReviewer is not { IsActive: true } || newReviewer.Roles.All(r => r.Role != UserRole.Manager))
            return ApiResponse.Fail("The new reviewer must be an active Manager.", 400);

        var previousReviewerId = training.AssignedReviewerId;
        training.AssignedReviewerId = newReviewer.Id;   
        training.UpdatedAt = clock.UtcNow;
        AddHistory(training, TrainingAction.ReviewerReassigned, training.Status, adminId, request.Reason);

        await auditLogRepository.AddAsync(new AuditLog
        {
            EmployeeId = adminId,
            Action = "TrainingReviewerReassigned",
            TargetEntity = nameof(TrainingRequest),
            TargetId = training.Id,
            Details = $"Reviewer {previousReviewerId} -> {newReviewer.Id}",
            CreatedAt = clock.UtcNow
        });
        await notificationService.NotifyAsync(newReviewer, "Training request assigned to you",
            $"{training.Reference} ({training.Title}) has been reassigned to you for approval.",
            relatedEntity: nameof(TrainingRequest), relatedEntityId: training.Id);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Reviewer reassigned.");
    }

    public async Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int id)
    {
        var training = await trainingRequestRepository.GetDetailsAsync(id);
        if (training == null)
            return ApiResponse.NotFound(NotFoundMessage);
        if (!TrainingAccess.CanView(training, userId, roles))
            return ApiResponse.Forbidden("You do not have access to this training request.");

        var attachments = await attachmentRepository.GetByParentAsync(id,
            AttachmentParentType.TrainingRequest, AttachmentParentType.TrainingEvidence);

        return ApiResponse.Success("Training request retrieved.", ToDetail(training, attachments));
    }

    public async Task<ApiResponse> GetMineAsync(int employeeId, TrainingRequestQuery query) =>
        ApiResponse.Paged("Training requests retrieved.",
            ToSummaries(await trainingRequestRepository.GetPagedForEmployeeAsync(employeeId, query)));

    public async Task<ApiResponse> GetPendingManagerAsync(int managerId, PagedRequest request) =>
        ApiResponse.Paged("Pending requests retrieved.",
            ToSummaries(await trainingRequestRepository.GetPagedPendingManagerAsync(managerId, request)));

    public async Task<ApiResponse> GetAllPendingManagerAsync(PagedRequest request) =>
        ApiResponse.Paged("Pending requests retrieved.",
            ToSummaries(await trainingRequestRepository.GetPagedPendingManagerAsync(null, request)));
    
    // ---- helpers ----

    private static ApiResponse? CheckOwnedDraft(TrainingRequest? training, int employeeId, string verb)
    {
        if (training == null)
            return ApiResponse.NotFound(NotFoundMessage);
        if (training.EmployeeId != employeeId)
            return ApiResponse.Forbidden("Only the owner can change this request.");
        if (training.Status != TrainingStatus.Draft)
            return ApiResponse.Conflict($"Only draft requests can be {verb}.");   // TRN-01.5
        return null;
    }

    private async Task<List<Employee>> GetCurrentReviewersAsync(TrainingRequest training)
    {
        var reviewers = training.Status switch
        {
            TrainingStatus.PendingManagerApproval when training.AssignedReviewer != null => [training.AssignedReviewer],
            TrainingStatus.PendingHRApproval or TrainingStatus.ReturnedToHR => await employeeRepository.GetActiveByRoleAsync(UserRole.HR),
            TrainingStatus.PendingFinanceApproval => await employeeRepository.GetActiveByRoleAsync(UserRole.Finance),
            _ => []
        };
        return reviewers.Where(r => r.Id != training.EmployeeId).ToList();
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

    private static void Apply(TrainingRequest training, SaveTrainingRequest request)
    {
        training.Title = request.Title.Trim();
        training.Provider = request.Provider.Trim();
        training.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        training.City = request.City.Trim();
        training.TrainingStartDate = request.TrainingStartDate;
        training.TrainingEndDate = request.TrainingEndDate;
        training.DepartureDate = request.DepartureDate;
        training.ReturnDate = request.ReturnDate;
        training.BusinessJustification = request.BusinessJustification.Trim();
        training.CourseFee = Math.Round(request.CourseFee, 2, MidpointRounding.AwayFromZero);
        training.CourseFeeCurrency = request.CourseFeeCurrency.Trim().ToUpperInvariant();
        training.FlightEstimate = Math.Round(request.FlightEstimate, 2, MidpointRounding.AwayFromZero);
        training.FlightEstimateCurrency = request.FlightEstimateCurrency.Trim().ToUpperInvariant();
    }
}