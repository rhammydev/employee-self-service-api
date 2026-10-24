using EmployeeSelfServiceAPI.DTOs.Auth;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ForgotPasswordRequest = EmployeeSelfServiceAPI.DTOs.Auth.ForgotPasswordRequest;
using LoginRequest = EmployeeSelfServiceAPI.DTOs.Auth.LoginRequest;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request) => 
        Respond(await auth.LoginAsync(request));
    
    [HttpPost("setup-password")]
    public async Task<IActionResult> SetupPassword(SetPasswordRequest request) => 
        Respond(await auth.SetupPasswordAsync(request));
    
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request) => 
        Respond(await auth.ForgotPasswordAsync(request));
    
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(SetPasswordRequest request) => 
        Respond(await auth.ResetPasswordAsync(request));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request) =>
        Respond(await auth.ChangePasswordAsync(User.GetEmployeeId(), request));
}