using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IAttachmentRepository
{
    Task AddAsync(Attachment attachment);
    Task<Attachment?> GetByIdAsync(int id);
    Task<List<Attachment>> GetByParentAsync(int parentId, params AttachmentParentType[] parentTypes);
    Task<List<Attachment>> GetByParentIdsAsync(AttachmentParentType parentType, IReadOnlyCollection<int> parentIds);
}