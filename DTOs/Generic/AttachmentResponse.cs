using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.DTOs.Generic;

public class AttachmentResponse
{
    public int Id { get; set; }
    public AttachmentParentType Kind { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string DownloadUrl => $"/api/v1/attachments/{Id}/download";
}