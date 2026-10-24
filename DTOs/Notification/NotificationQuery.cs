using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Notification;

public class NotificationQuery : PagedRequest
{
    public bool? IsRead { get; set; }
}
