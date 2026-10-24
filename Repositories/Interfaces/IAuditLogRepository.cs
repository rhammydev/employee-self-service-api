using EmployeeSelfServiceAPI.Domain.Entities.Common;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
}