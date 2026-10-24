using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Notification;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<Notification?> GetByIdAsync(int id);
    Task<PagedResult<Notification>> GetPagedForEmployeeAsync(int employeeId, NotificationQuery query);
    Task<int> CountUnreadAsync(int employeeId);

    Task<int> MarkAllReadAsync(int employeeId, DateTime readAt);
}
