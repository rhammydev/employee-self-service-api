using EmployeeSelfServiceAPI.Domain.Entities.Common;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IEmailQueueRepository
{
    Task AddAsync(EmailMessage message);
}