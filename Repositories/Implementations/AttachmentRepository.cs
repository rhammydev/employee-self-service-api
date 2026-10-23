using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class AttachmentRepository(AppDbContext context) : IAttachmentRepository
{
    public async Task AddAsync(Attachment attachment) =>
        await context.Attachments.AddAsync(attachment);

    public Task<Attachment?> GetByIdAsync(int id) =>
        context.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

    public Task<List<Attachment>> GetByParentAsync(int parentId, params AttachmentParentType[] parentTypes) =>
        context.Attachments.AsNoTracking()
            .Where(a => a.ParentId == parentId && parentTypes.Contains(a.ParentType))
            .OrderBy(a => a.UploadedAt)
            .ToListAsync();
}