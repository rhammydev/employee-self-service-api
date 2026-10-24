using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Repositories.Interfaces;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class AuditLogRepository(AppDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog log) =>
        await context.AuditLogs.AddAsync(log);
}