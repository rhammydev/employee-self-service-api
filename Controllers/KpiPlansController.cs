using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Performance;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/kpi-plans")]
[Authorize]
public class KpiPlansController(IKpiPlanService plans, IKpiProgressService progress) : ControllerBase
{
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost("/api/v1/review-periods")]
    public async Task<IActionResult> CreatePeriod(CreateReviewPeriodRequest request) =>
        Respond(await plans.CreatePeriodAsync(User.GetEmployeeId(), request));

    [HttpGet("/api/v1/review-periods")]
    public async Task<IActionResult> GetPeriods([FromQuery] PagedRequest request) =>
        Respond(await plans.GetPeriodsAsync(request));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost]
    public async Task<IActionResult> CreatePlan(CreateKpiPlanRequest request) =>
        Respond(await plans.CreatePlanAsync(User.GetEmployeeId(), request));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] KpiPlanQuery query) =>
        Respond(await plans.GetAllAsync(query));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost("{id:int}/items")]
    public async Task<IActionResult> AddItem(int id, SaveKpiItemRequest request) =>
        Respond(await plans.AddItemAsync(User.GetEmployeeId(), id, request));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPut("{id:int}/items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(int id, int itemId, SaveKpiItemRequest request) =>
        Respond(await plans.UpdateItemAsync(User.GetEmployeeId(), id, itemId, request));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpDelete("{id:int}/items/{itemId:int}")]
    public async Task<IActionResult> DeleteItem(int id, int itemId) =>
        Respond(await plans.DeleteItemAsync(User.GetEmployeeId(), id, itemId));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, ActivateKpiPlanRequest request) =>
        Respond(await plans.ActivateAsync(User.GetEmployeeId(), id, request));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost("{id:int}/reopen")]
    public async Task<IActionResult> Reopen(int id, ReopenKpiPlanRequest request) =>
        Respond(await plans.ReopenAsync(User.GetEmployeeId(), id, request));

    // ---- Staff (KPI-02) ----
    [HttpGet("my")]
    public async Task<IActionResult> GetMine([FromQuery] KpiPlanQuery query) =>
        Respond(await progress.GetMineAsync(User.GetEmployeeId(), query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        Respond(await progress.GetByIdAsync(User.GetEmployeeId(), User.GetRoles(), id));

    [HttpPut("{id:int}/items/{itemId:int}/progress")]
    public async Task<IActionResult> UpdateProgress(int id, int itemId, UpdateKpiProgressRequest request) =>
        Respond(await progress.UpdateProgressAsync(User.GetEmployeeId(), id, itemId, request));

    [HttpPost("{id:int}/items/{itemId:int}/evidence")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadEvidence(int id, int itemId, [FromForm] UploadFileRequest request) =>
        Respond(await progress.UploadEvidenceAsync(User.GetEmployeeId(), id, itemId, request.File));

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id) =>
        Respond(await progress.SubmitAsync(User.GetEmployeeId(), id));

    [Authorize(Roles = nameof(UserRole.Manager))]
    [HttpGet("pending-review")]
    public async Task<IActionResult> GetPendingReview([FromQuery] PagedRequest request) =>
        Respond(await progress.GetPendingReviewAsync(User.GetEmployeeId(), request));

    [Authorize(Roles = nameof(UserRole.Manager))]
    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, KpiReviewRequest request) =>
        Respond(await progress.ReviewAsync(User.GetEmployeeId(), id, request));
}
