using EmployeeSelfServiceAPI.Domain.Entities.Common;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
}