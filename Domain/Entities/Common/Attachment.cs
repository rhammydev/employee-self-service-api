using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Common;

public class Attachment
{
    public int Id { get; set; }
    public AttachmentParentType ParentType { get; set; }
    public int ParentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int UploadedById { get; set; }
    public Employee? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}