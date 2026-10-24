using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/course-assignments")]
[Authorize]
public class CourseAssignmentsController(ICourseAssignmentService assignments) : ControllerBase
{
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);

    [HttpGet("my")]
    public async Task<IActionResult> GetMine([FromQuery] CourseAssignmentQuery query) =>
        Respond(await assignments.GetMineAsync(User.GetEmployeeId(), query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        Respond(await assignments.GetByIdAsync(User.GetEmployeeId(), User.GetRoles(), id));

    [HttpPost("{id:int}/lessons/{lessonId:int}/complete")]
    public async Task<IActionResult> CompleteLesson(int id, int lessonId) =>
        Respond(await assignments.CompleteLessonAsync(User.GetEmployeeId(), id, lessonId));

    [HttpPost("{id:int}/attempts")]
    public async Task<IActionResult> SubmitAttempt(int id, SubmitAttemptRequest request) =>
        Respond(await assignments.SubmitAttemptAsync(User.GetEmployeeId(), id, request));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost("{id:int}/reset")]
    public async Task<IActionResult> Reset(int id, ReasonRequest request) =>
        Respond(await assignments.ResetAsync(User.GetEmployeeId(), id, request));
}
