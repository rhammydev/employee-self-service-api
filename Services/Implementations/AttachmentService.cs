using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class AttachmentService(
    IAttachmentRepository attachmentRepository,
    ITrainingRequestRepository trainingRequestRepository,
    IKpiRepository kpiRepository,
    IOpportunityRepository opportunityRepository,
    IFileStorageService fileStorageService): IAttachmentService
{
    public async Task<(ApiResponse? Error, FileDownload? File)> DownloadAsync(int userId, IReadOnlyList<UserRole> roles, int attachmentId)
    {
        var attachment = await attachmentRepository.GetByIdAsync(attachmentId);
        if (attachment == null)
            return (ApiResponse.NotFound("Attachment not found."), null);

        if (!await CanAccessParentAsync(attachment, userId, roles))
            return (ApiResponse.Forbidden("You do not have access to this file."), null);

        var stream = fileStorageService.OpenRead(attachment.StoredFileName);
        if (stream == null)
            return (ApiResponse.NotFound("The file is no longer available."), null);

        return (null, new FileDownload(stream, attachment.ContentType, attachment.OriginalFileName));
    }
    
    private async Task<bool> CanAccessParentAsync(Attachment attachment, int userId, IReadOnlyList<UserRole> roles) =>
        attachment.ParentType switch
        {
            AttachmentParentType.TrainingRequest or AttachmentParentType.TrainingEvidence =>
                await trainingRequestRepository.GetDetailsAsync(attachment.ParentId) is { } request
                && TrainingAccess.CanView(request, userId, roles),

            AttachmentParentType.KpiItem =>
                await kpiRepository.GetPlanByItemIdAsync(attachment.ParentId) is { } plan
                && KpiAccess.CanView(plan, userId, roles),

            AttachmentParentType.OpportunityApplication =>
                await opportunityRepository.GetApplicationDetailsAsync(attachment.ParentId) is { } application
                && (application.EmployeeId == userId || roles.Contains(UserRole.HR)),

            _ => false
        };
}