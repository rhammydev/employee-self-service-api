using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var response = await dashboard.GetMineAsync(User.GetEmployeeId(), User.GetRoles());
        return StatusCode(response.HttpStatusCode, response);
    }
}

// Lets Admin trigger the daily reminder job on demand, e.g. to show reruns create no duplicates
[ApiController]
[Route("api/v1/reminders")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class RemindersController(IReminderService reminders) : ControllerBase
{
    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        var result = await reminders.RunAsync(cancellationToken);
        var response = ApiResponse.Success(
            $"Reminder run for {result.ReminderDate:yyyy-MM-dd}: {result.Created} created, {result.SkippedAsDuplicate} skipped as duplicates.",
            result);
        return StatusCode(response.HttpStatusCode, response);
    }
}
