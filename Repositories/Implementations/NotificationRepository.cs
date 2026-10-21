using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Repositories.Interfaces;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification) =>
        await context.Notifications.AddAsync(notification);
}