using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Training;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/training-requests")]
[Authorize]
public class TrainingRequestsController(ITrainingRequestService training) : ControllerBase
{
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);

    // Staff (every active employee)
    [HttpPost]
    public async Task<IActionResult> Create(SaveTrainingRequest request) =>
        Respond(await training.CreateAsync(User.GetEmployeeId(), request));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveTrainingRequest request) =>
        Respond(await training.UpdateAsync(User.GetEmployeeId(), id, request));

    [HttpPost("{id:int}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]   // a little over 5 MB for multipart overhead; the service enforces 5 MB
    public async Task<IActionResult> UploadSupportingFile(int id, [FromForm] UploadFileRequest request) =>
        Respond(await training.UploadSupportingFileAsync(User.GetEmployeeId(), id, request.File));

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id) =>
        Respond(await training.SubmitAsync(User.GetEmployeeId(), id));

    [HttpPost("{id:int}/withdraw")]
    public async Task<IActionResult> Withdraw(int id, ReasonRequest request) =>
        Respond(await training.WithdrawAsync(User.GetEmployeeId(), id, request));

    [HttpGet("my")]
    public async Task<IActionResult> GetMine([FromQuery] TrainingRequestQuery query) =>
        Respond(await training.GetMineAsync(User.GetEmployeeId(), query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        Respond(await training.GetByIdAsync(User.GetEmployeeId(), User.GetRoles(), id));

    // Manager
    [Authorize(Roles = nameof(UserRole.Manager))]
    [HttpGet("pending-manager")]
    public async Task<IActionResult> GetPendingManager([FromQuery] PagedRequest request) =>
        Respond(await training.GetPendingManagerAsync(User.GetEmployeeId(), request));

    [Authorize(Roles = nameof(UserRole.Manager))]
    [HttpPost("{id:int}/manager-decision")]
    public async Task<IActionResult> ManagerDecision(int id, ManagerDecisionRequest request) =>
        Respond(await training.ManagerDecisionAsync(User.GetEmployeeId(), id, request));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("pending-manager/all")]
    public async Task<IActionResult> GetAllPendingManager([FromQuery] PagedRequest request) =>
        Respond(await training.GetAllPendingManagerAsync(request));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("{id:int}/reassign-reviewer")]
    public async Task<IActionResult> ReassignReviewer(int id, ReassignReviewerRequest request) =>
        Respond(await training.ReassignReviewerAsync(User.GetEmployeeId(), id, request));
}