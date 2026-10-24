using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Opportunities;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Opportunities;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class OpportunityService(
    IOpportunityRepository opportunityRepository,
    IOrganizationRepository organizationRepository,
    IEmployeeRepository employeeRepository,
    IAttachmentRepository attachmentRepository,
    IFileStorageService fileStorageService,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IAppClock clock) : IOpportunityService
{
    private const string OpportunityNotFound = "Opportunity not found.";
    private const string ApplicationNotFound = "Application not found.";


    public async Task<ApiResponse> CreateAsync(int hrId, SaveOpportunityRequest request)
    {
        var error = await ValidateReferencesAsync(request);
        if (error != null) return error;

        var now = clock.UtcNow;
        var opportunity = new Opportunity
        {
            Status = OpportunityStatus.Draft,
            CreatedById = hrId,
            CreatedAt = now
        };
        Apply(opportunity, request);
        await opportunityRepository.AddAsync(opportunity);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Opportunity created as draft.", new { opportunity.Id, opportunity.Status }, 201);
    }

    public async Task<ApiResponse> UpdateAsync(int opportunityId, SaveOpportunityRequest request)
    {
        var opportunity = await opportunityRepository.GetForUpdateAsync(opportunityId);
        if (opportunity == null)
            return ApiResponse.NotFound(OpportunityNotFound);
        if (opportunity.Status == OpportunityStatus.Closed)
            return ApiResponse.Conflict("A closed opportunity cannot be changed. Create a new one.");

        var error = await ValidateReferencesAsync(request);
        if (error != null) return error;

        // A published closing time must stay in the future
        if (opportunity.Status == OpportunityStatus.Published && request.ClosingAt <= clock.UtcNow)
            return ApiResponse.Fail("The closing time of a published opportunity must be in the future.", 400);

        foreach (var item in opportunity.AllowedDepartments.ToList())
            opportunityRepository.RemoveAllowedDepartment(item);
        foreach (var item in opportunity.AllowedGrades.ToList())
            opportunityRepository.RemoveAllowedGrade(item);
        opportunity.AllowedDepartments.Clear();
        opportunity.AllowedGrades.Clear();

        Apply(opportunity, request);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Opportunity updated.");
    }

    public async Task<ApiResponse> PublishAsync(int opportunityId)
    {
        var opportunity = await opportunityRepository.GetForUpdateAsync(opportunityId);
        if (opportunity == null)
            return ApiResponse.NotFound(OpportunityNotFound);
        if (opportunity.Status != OpportunityStatus.Draft)
            return ApiResponse.Conflict("Only draft opportunities can be published.");

        if (opportunity.ClosingAt <= clock.UtcNow)
            return ApiResponse.Fail("The closing time must be in the future to publish.", 400, ResponseCodes.BadRequest,
                [new ApiError("closingAt", "The closing time must be in the future to publish.")]);

        opportunity.Status = OpportunityStatus.Published;
        opportunity.PublishedAt = clock.UtcNow;
        opportunity.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Opportunity published.");
    }

    public async Task<ApiResponse> CloseAsync(int opportunityId)
    {
        var opportunity = await opportunityRepository.GetForUpdateAsync(opportunityId);
        if (opportunity == null)
            return ApiResponse.NotFound(OpportunityNotFound);
        if (opportunity.Status != OpportunityStatus.Published)
            return ApiResponse.Conflict(opportunity.Status == OpportunityStatus.Closed
                ? "This opportunity is already closed."
                : "Only published opportunities can be closed.");

        opportunity.Status = OpportunityStatus.Closed;
        opportunity.ClosedAt = clock.UtcNow;
        opportunity.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Opportunity closed.");
    }

    public async Task<ApiResponse> GetAllAsync(int userId, IReadOnlyList<UserRole> roles, OpportunityQuery query)
    {
        var page = await opportunityRepository.GetPagedAsync(query, publishedOnly: !roles.Contains(UserRole.HR), clock.UtcNow);
        return ApiResponse.Paged("Opportunities retrieved.", new PagedResult<OpportunitySummaryResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(o => FillSummary(new OpportunitySummaryResponse(), o)).ToList()
        });
    }

    public async Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int opportunityId)
    {
        var opportunity = await opportunityRepository.GetDetailsAsync(opportunityId);

        if (opportunity == null || (!roles.Contains(UserRole.HR) && opportunity.Status != OpportunityStatus.Published))
            return ApiResponse.NotFound(OpportunityNotFound);

        var viewer = await employeeRepository.GetByIdAsync(userId);
        var myApplication = await opportunityRepository.GetApplicationIdForEmployeeAsync(opportunityId, userId);

        var detail = FillSummary(new OpportunityDetailResponse(), opportunity);
        detail.DepartmentId = opportunity.DepartmentId;
        detail.Description = opportunity.Description;
        detail.Eligibility = opportunity.Eligibility;
        detail.AllowedDepartments = opportunity.AllowedDepartments
            .Select(a => new IdNameResponse { Id = a.DepartmentId, Name = a.Department?.Name ?? string.Empty }).ToList();
        detail.AllowedGrades = opportunity.AllowedGrades
            .Select(a => new IdNameResponse { Id = a.GradeId, Name = a.Grade == null ? string.Empty : $"{a.Grade.Code} {a.Grade.Name}" }).ToList();
        detail.IsEligible = viewer == null ? null : IsEligible(opportunity, viewer);
        detail.MyApplicationId = myApplication?.Id;

        return ApiResponse.Success("Opportunity retrieved.", detail);
    }

    public async Task<ApiResponse> GetApplicationsAsync(int opportunityId, ApplicationQuery query)
    {
        var page = await opportunityRepository.GetPagedApplicationsForOpportunityAsync(opportunityId, query);
        return ApiResponse.Paged("Applications retrieved.", new PagedResult<ApplicationSummaryResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(a => FillApplication(new ApplicationSummaryResponse(), a)).ToList()
        });
    }


    public async Task<ApiResponse> ApplyAsync(int employeeId, int opportunityId, ApplyRequest request)
    {
        var opportunity = await opportunityRepository.GetDetailsAsync(opportunityId);
        if (opportunity == null || opportunity.Status == OpportunityStatus.Draft)
            return ApiResponse.NotFound(OpportunityNotFound);

        if (opportunity.Status != OpportunityStatus.Published || opportunity.ClosingAt <= clock.UtcNow)
            return ApiResponse.Conflict("This opportunity is closed for applications.");

        var employee = await employeeRepository.GetByIdAsync(employeeId);
        if (employee == null)
            return ApiResponse.NotFound("Employee not found.");

        if (!IsEligible(opportunity, employee))
            return ApiResponse.Forbidden("You are not eligible for this opportunity (department or grade).");

        if (await opportunityRepository.GetApplicationIdForEmployeeAsync(opportunityId, employeeId) != null)
            return ApiResponse.Conflict("You have already applied for this opportunity.");

        (AllowedFileType? Type, string? Error) cv = (null, null);
        if (request.Cv != null)
        {
            cv = await fileStorageService.ValidateAsync(request.Cv, FileTypes.PdfOnly);   // PDF only, 5 MB
            if (cv.Error != null)
                return ApiResponse.Fail(cv.Error, 400, ResponseCodes.BadRequest, [new ApiError("cv", cv.Error)]);
        }

        var now = clock.UtcNow;
        var application = new OpportunityApplication
        {
            OpportunityId = opportunityId,
            EmployeeId = employeeId,
            SupportingStatement = request.SupportingStatement.Trim(),
            Status = ApplicationStatus.Submitted,
            DepartmentIdAtApply = employee.DepartmentId,
            GradeIdAtApply = employee.GradeId,
            EligibilityCheckedAt = now,
            SubmittedAt = now,
            UpdatedAt = now
        };
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            FromStatus = null,
            ToStatus = ApplicationStatus.Submitted,
            ActorId = employeeId,
            CreatedAt = now
        });
        await opportunityRepository.AddApplicationAsync(application);

        await using var transaction = await unitOfWork.BeginTransactionAsync();
        await unitOfWork.SaveChangesAsync();   

        if (request.Cv != null)
        {
            await attachmentRepository.AddAsync(new Attachment
            {
                ParentType = AttachmentParentType.OpportunityApplication,
                ParentId = application.Id,
                OriginalFileName = Path.GetFileName(request.Cv.FileName),
                StoredFileName = await fileStorageService.SaveAsync(request.Cv, cv.Type!),
                ContentType = cv.Type!.ContentType,
                SizeBytes = request.Cv.Length,
                UploadedById = employeeId,
                UploadedAt = now
            });
        }

        await notificationService.NotifyAsync(employee, "Application received",
            $"Your application for \"{opportunity.Title}\" was submitted.",
            relatedEntity: nameof(OpportunityApplication), relatedEntityId: application.Id);
        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        return ApiResponse.Success("Application submitted.", new { application.Id, application.Status }, 201);
    }

    public async Task<ApiResponse> GetMyApplicationsAsync(int employeeId, ApplicationQuery query)
    {
        var page = await opportunityRepository.GetPagedApplicationsForEmployeeAsync(employeeId, query);
        return ApiResponse.Paged("Applications retrieved.", new PagedResult<ApplicationSummaryResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(a => FillApplication(new ApplicationSummaryResponse(), a)).ToList()
        });
    }

    public async Task<ApiResponse> GetApplicationAsync(int userId, IReadOnlyList<UserRole> roles, int applicationId)
    {
        var application = await opportunityRepository.GetApplicationDetailsAsync(applicationId);
        if (application == null)
            return ApiResponse.NotFound(ApplicationNotFound);
        if (application.EmployeeId != userId && !roles.Contains(UserRole.HR))
            return ApiResponse.Forbidden("You do not have access to this application.");

        var attachments = await attachmentRepository.GetByParentAsync(applicationId, AttachmentParentType.OpportunityApplication);

        var detail = FillApplication(new ApplicationDetailResponse(), application);
        detail.SupportingStatement = application.SupportingStatement;
        detail.DepartmentAtApply = application.DepartmentAtApply?.Name ?? string.Empty;
        detail.GradeAtApply = application.GradeAtApply == null ? string.Empty : application.GradeAtApply.Code;
        detail.EligibilityCheckedAt = application.EligibilityCheckedAt;
        detail.Attachments = attachments.Select(a => new AttachmentResponse
        {
            Id = a.Id,
            Kind = a.ParentType,
            FileName = a.OriginalFileName,
            ContentType = a.ContentType,
            SizeBytes = a.SizeBytes,
            UploadedAt = a.UploadedAt
        }).ToList();
        detail.History = application.StatusHistory.OrderBy(h => h.CreatedAt).Select(h => new ApplicationHistoryResponse
        {
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ActorName = Name(h.Actor),
            Comment = h.Comment,
            CreatedAt = h.CreatedAt
        }).ToList();

        return ApiResponse.Success("Application retrieved.", detail);
    }

    public async Task<ApiResponse> WithdrawAsync(int employeeId, int applicationId)
    {
        var application = await opportunityRepository.GetApplicationForUpdateAsync(applicationId);
        if (application == null)
            return ApiResponse.NotFound(ApplicationNotFound);
        if (application.EmployeeId != employeeId)
            return ApiResponse.Forbidden("You can only withdraw your own application.");

        // VAC-02.6
        if (!ApplicationWorkflow.CanWithdraw(application.Status))
            return ApiResponse.Conflict($"An application in {application.Status} status cannot be withdrawn.");

        ChangeStatus(application, ApplicationStatus.Withdrawn, employeeId, null);
        await notificationService.NotifyAsync(application.Employee!, "Application withdrawn",
            $"Your application for \"{application.Opportunity!.Title}\" was withdrawn.",
            relatedEntity: nameof(OpportunityApplication), relatedEntityId: application.Id);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Application withdrawn.");
    }

    public async Task<ApiResponse> UpdateApplicationStatusAsync(int hrId, int applicationId, UpdateApplicationStatusRequest request)
    {
        var application = await opportunityRepository.GetApplicationForUpdateAsync(applicationId);
        if (application == null)
            return ApiResponse.NotFound(ApplicationNotFound);
        if (application.EmployeeId == hrId)
            return ApiResponse.Forbidden("You cannot decide on your own application.");

        if (!ApplicationWorkflow.CanHrMove(application.Status, request.Status))
            return ApiResponse.Conflict($"An application cannot move from {application.Status} to {request.Status}.");

        ChangeStatus(application, request.Status, hrId, request.Comment?.Trim());

        await notificationService.NotifyAsync(application.Employee!, $"Application {request.Status.ToString().ToLowerInvariant()}",
            $"Your application for \"{application.Opportunity!.Title}\" is now {request.Status}."
            + (string.IsNullOrWhiteSpace(request.Comment) ? string.Empty : $" Comment: {request.Comment.Trim()}"),
            relatedEntity: nameof(OpportunityApplication), relatedEntityId: application.Id);

        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success($"Application moved to {request.Status}.");
    }

    // ---- helpers ----

    private async Task<ApiResponse?> ValidateReferencesAsync(SaveOpportunityRequest request)
    {
        var errors = new List<ApiError>();

        if (!await organizationRepository.DepartmentExistsAsync(request.DepartmentId))
            errors.Add(new ApiError("departmentId", "Department does not exist."));

        var missingDepartments = await organizationRepository.GetMissingDepartmentIdsAsync(request.AllowedDepartmentIds);
        if (missingDepartments.Count > 0)
            errors.Add(new ApiError("allowedDepartmentIds", $"Unknown department ids: {string.Join(", ", missingDepartments)}."));

        var missingGrades = await organizationRepository.GetMissingGradeIdsAsync(request.AllowedGradeIds);
        if (missingGrades.Count > 0)
            errors.Add(new ApiError("allowedGradeIds", $"Unknown grade ids: {string.Join(", ", missingGrades)}."));

        return errors.Count == 0 ? null : ApiResponse.Fail("Validation failed.", 400, ResponseCodes.BadRequest, errors);
    }

    private void Apply(Opportunity opportunity, SaveOpportunityRequest request)
    {
        opportunity.Title = request.Title.Trim();
        opportunity.Type = request.Type;
        opportunity.DepartmentId = request.DepartmentId;
        opportunity.Description = request.Description.Trim();
        opportunity.Eligibility = request.Eligibility?.Trim();
        opportunity.ClosingAt = DateTime.SpecifyKind(request.ClosingAt.ToUniversalTime(), DateTimeKind.Utc);
        opportunity.UpdatedAt = clock.UtcNow;

        foreach (var departmentId in request.AllowedDepartmentIds)
            opportunity.AllowedDepartments.Add(new OpportunityAllowedDepartment { DepartmentId = departmentId });
        foreach (var gradeId in request.AllowedGradeIds)
            opportunity.AllowedGrades.Add(new OpportunityAllowedGrade { GradeId = gradeId });
    }

    private static bool IsEligible(Opportunity opportunity, Employee employee) =>
        ApplicationWorkflow.IsEligible(
            opportunity.AllowedDepartments.Select(a => a.DepartmentId).ToList(),
            opportunity.AllowedGrades.Select(a => a.GradeId).ToList(),
            employee.DepartmentId, employee.GradeId);

    private void ChangeStatus(OpportunityApplication application, ApplicationStatus to, int actorId, string? comment)
    {
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            FromStatus = application.Status,
            ToStatus = to,
            ActorId = actorId,
            Comment = comment,
            CreatedAt = clock.UtcNow
        });
        application.Status = to;
        application.UpdatedAt = clock.UtcNow;
    }

    private T FillSummary<T>(T response, Opportunity o) where T : OpportunitySummaryResponse
    {
        response.Id = o.Id;
        response.Title = o.Title;
        response.Type = o.Type;
        response.DepartmentName = o.Department?.Name ?? string.Empty;
        response.Status = o.Status;
        response.ClosingAt = o.ClosingAt;
        response.IsOpen = o.Status == OpportunityStatus.Published && o.ClosingAt > clock.UtcNow;
        response.PublishedAt = o.PublishedAt;
        return response;
    }

    private static T FillApplication<T>(T response, OpportunityApplication a) where T : ApplicationSummaryResponse
    {
        response.Id = a.Id;
        response.OpportunityId = a.OpportunityId;
        response.OpportunityTitle = a.Opportunity?.Title ?? string.Empty;
        response.EmployeeId = a.EmployeeId;
        response.EmployeeName = Name(a.Employee);
        response.Status = a.Status;
        response.SubmittedAt = a.SubmittedAt;
        response.UpdatedAt = a.UpdatedAt;
        return response;
    }

    private static string Name(Employee? e) => e == null ? string.Empty : $"{e.FirstName} {e.LastName}";
}
