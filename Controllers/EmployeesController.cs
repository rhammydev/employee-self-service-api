using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Employee;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public class EmployeesController(IEmployeeService employees) : ControllerBase
{
    private const string HrOrAdmin = $"{nameof(UserRole.HR)},{nameof(UserRole.Admin)}";
    
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);
    
    // Self-service
    [HttpGet("me")]
    public async Task<IActionResult> GetMe() =>
        Respond(await employees.GetMyProfileAsync(User.GetEmployeeId()));

    [HttpPut("me/phone")]
    public async Task<IActionResult> UpdateMyPhone(UpdatePhoneRequest request) =>
        Respond(await employees.UpdateMyPhoneAsync(User.GetEmployeeId(), request));

    [Authorize(Roles = nameof(UserRole.Manager))]
    [HttpGet("my-team")]
    public async Task<IActionResult> GetMyTeam([FromQuery] PagedRequest request) =>
        Respond(await employees.GetMyTeamAsync(User.GetEmployeeId(), request));
    
    [Authorize(Roles = HrOrAdmin)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeQuery query) =>
        Respond(await employees.GetAllAsync(query));

    [Authorize(Roles = HrOrAdmin)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        Respond(await employees.GetByIdAsync(id));

    [Authorize(Roles = HrOrAdmin)]
    [HttpPut("{id:int}/employment")]
    public async Task<IActionResult> UpdateEmployment(int id, UpdateEmploymentRequest request) =>
        Respond(await employees.UpdateEmploymentAsync(User.GetEmployeeId(), id, request));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeRequest request) =>
        Respond(await employees.CreateAsync(User.GetEmployeeId(), request));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("{id:int}/resend-setup-link")]
    public async Task<IActionResult> ResendSetupLink(int id) =>
        Respond(await employees.ResendSetupLinkAsync(User.GetEmployeeId(), id));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:int}/roles")]
    public async Task<IActionResult> UpdateRoles(int id, UpdateRolesRequest request) =>
        Respond(await employees.UpdateRolesAsync(User.GetEmployeeId(), id, request));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateStatusRequest request) =>
        Respond(await employees.UpdateStatusAsync(User.GetEmployeeId(), id, request));
}