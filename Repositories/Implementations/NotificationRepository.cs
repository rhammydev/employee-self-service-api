using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Notification;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification) =>
        await context.Notifications.AddAsync(notification);

    public Task<Notification?> GetByIdAsync(int id) =>
        context.Notifications.FirstOrDefaultAsync(n => n.Id == id);

    public Task<PagedResult<Notification>> GetPagedForEmployeeAsync(int employeeId, NotificationQuery query)
    {
        var notifications = context.Notifications.AsNoTracking().Where(n => n.EmployeeId == employeeId);

        if (query.IsRead.HasValue)
            notifications = notifications.Where(n => n.IsRead == query.IsRead);

        return notifications.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).ToPagedResultAsync(query);
    }

    public Task<int> CountUnreadAsync(int employeeId) =>
        context.Notifications.CountAsync(n => n.EmployeeId == employeeId && !n.IsRead);

    public Task<int> MarkAllReadAsync(int employeeId, DateTime readAt) =>
        context.Notifications
            .Where(n => n.EmployeeId == employeeId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, readAt));
}
