using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Opportunities;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/opportunities")]
[Authorize]
public class OpportunitiesController(IOpportunityService opportunities) : ControllerBase
{
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost]
    public async Task<IActionResult> Create(SaveOpportunityRequest request) =>
        Respond(await opportunities.CreateAsync(User.GetEmployeeId(), request));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveOpportunityRequest request) =>
        Respond(await opportunities.UpdateAsync(id, request));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id) =>
        Respond(await opportunities.PublishAsync(id));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id) =>
        Respond(await opportunities.CloseAsync(id));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpGet("{id:int}/applications")]
    public async Task<IActionResult> GetApplications(int id, [FromQuery] ApplicationQuery query) =>
        Respond(await opportunities.GetApplicationsAsync(id, query));

    // ---- Everyone: staff see Published only, HR sees all ----
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] OpportunityQuery query) =>
        Respond(await opportunities.GetAllAsync(User.GetEmployeeId(), User.GetRoles(), query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        Respond(await opportunities.GetByIdAsync(User.GetEmployeeId(), User.GetRoles(), id));

    [HttpPost("{id:int}/applications")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Apply(int id, [FromForm] ApplyRequest request) =>
        Respond(await opportunities.ApplyAsync(User.GetEmployeeId(), id, request));
}

[ApiController]
[Route("api/v1/applications")]
[Authorize]
public class ApplicationsController(IOpportunityService opportunities) : ControllerBase
{
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);

    [HttpGet("my")]
    public async Task<IActionResult> GetMine([FromQuery] ApplicationQuery query) =>
        Respond(await opportunities.GetMyApplicationsAsync(User.GetEmployeeId(), query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        Respond(await opportunities.GetApplicationAsync(User.GetEmployeeId(), User.GetRoles(), id));

    [HttpPost("{id:int}/withdraw")]
    public async Task<IActionResult> Withdraw(int id) =>
        Respond(await opportunities.WithdrawAsync(User.GetEmployeeId(), id));

    [Authorize(Roles = nameof(UserRole.HR))]
    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateApplicationStatusRequest request) =>
        Respond(await opportunities.UpdateApplicationStatusAsync(User.GetEmployeeId(), id, request));
}
