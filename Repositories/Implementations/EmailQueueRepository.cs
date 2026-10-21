using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Repositories.Interfaces;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class EmailQueueRepository(AppDbContext context) : IEmailQueueRepository
{
    public async Task AddAsync(EmailMessage message) =>
        await context.EmailMessages.AddAsync(message);
}