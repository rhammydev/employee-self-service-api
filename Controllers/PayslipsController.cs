using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Payroll;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/payslips")]
[Authorize]
public class PayslipsController(IPayslipService payslips) : ControllerBase
{
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);

    [Authorize(Roles = nameof(UserRole.Finance))]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePayslipRequest request) =>
        Respond(await payslips.CreateAsync(User.GetEmployeeId(), request));

    [Authorize(Roles = nameof(UserRole.Finance))]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PayslipQuery query) =>
        Respond(await payslips.GetAllAsync(query));

    [Authorize(Roles = nameof(UserRole.Finance))]
    [HttpPut("{id:int}/draft")]
    public async Task<IActionResult> UpdateDraft(int id, PayslipAmountsRequest request) =>
        Respond(await payslips.UpdateDraftAsync(User.GetEmployeeId(), id, request));

    [Authorize(Roles = nameof(UserRole.Finance))]
    [HttpPost("{id:int}/revisions")]
    public async Task<IActionResult> CreateRevision(int id, CreateRevisionRequest request) =>
        Respond(await payslips.CreateRevisionAsync(User.GetEmployeeId(), id, request));

    [Authorize(Roles = nameof(UserRole.Finance))]
    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id) =>
        Respond(await payslips.PublishAsync(User.GetEmployeeId(), id));

    [HttpGet("my")]
    public async Task<IActionResult> GetMine([FromQuery] PayslipQuery query) =>
        Respond(await payslips.GetMineAsync(User.GetEmployeeId(), query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        Respond(await payslips.GetByIdAsync(User.GetEmployeeId(), User.GetRoles(), id));

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id, [FromQuery] int? version)
    {
        var (error, file) = await payslips.GetPdfAsync(User.GetEmployeeId(), User.GetRoles(), id, version);
        if (error != null)
            return Respond(error);

        return File(file!.Content, file.ContentType, file.FileName);
    }
}
