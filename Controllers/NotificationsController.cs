using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Notification;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(INotificationService notifications) : ControllerBase
{
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] NotificationQuery query) =>
        Respond(await notifications.GetMineAsync(User.GetEmployeeId(), query));

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id) =>
        Respond(await notifications.MarkReadAsync(User.GetEmployeeId(), id));

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead() =>
        Respond(await notifications.MarkAllReadAsync(User.GetEmployeeId()));
}
