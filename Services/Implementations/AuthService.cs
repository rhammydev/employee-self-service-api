using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.Domain.Settings;
using EmployeeSelfServiceAPI.DTOs.Auth;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;
using EmployeeSelfServiceAPI.Utilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class AuthService(
    IEmployeeRepository employeeRepository,
    IAccountTokenRepository accountTokenRepository,
    IAuditLogRepository auditLogRepository,
    INotificationService notificationService,
    IEmailQueueRepository emailQueueRepository,
    IUnitOfWork unitOfWork,
    IOptions<JwtSettings> jwtOptions,
    IOptions<AuthSettings> authOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidLoginMessage = "Invalid email or password.";

    private const string SetupEmailTitle = "Set up your account";
    private const string ResetEmailTitle = "Password reset requested";

    private readonly JwtSettings _jwtSettings = jwtOptions.Value;
    private readonly AuthSettings _authSettings = authOptions.Value;
    
    public async Task<ApiResponse> LoginAsync(LoginRequest request)
    {
        var now = DateTime.UtcNow;
        var email = request.Email.Trim().ToLowerInvariant();
        var employee = await employeeRepository.GetByEmailWithRolesAsync(email);

        if (employee == null) return ApiResponse.Fail(InvalidLoginMessage, 401, ResponseCodes.Unauthorized);

        if (employee.LockoutEndAt > now)
        {
            await AuditAsync(employee.Id, "LoginBlocked", "Account Locked");
            await unitOfWork.SaveChangesAsync();
            return ApiResponse.Fail(InvalidLoginMessage, 401, ResponseCodes.Unauthorized);
        };

        if (employee.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
        {
           RegisterFailedLogin(employee, now);
           await AuditAsync(employee.Id, "LoginFailed", employee.LockoutEndAt > now ? "Wrong password; account locked" : "Wrong password");
           await unitOfWork.SaveChangesAsync();
           return ApiResponse.Fail(InvalidLoginMessage, 401, ResponseCodes.Unauthorized);
        }

        if (!employee.IsActive)
        {
            await AuditAsync(employee.Id, "LoginBlocked", "Account inactive");
            await unitOfWork.SaveChangesAsync();
            return ApiResponse.Fail(InvalidLoginMessage, 401, ResponseCodes.Unauthorized);
        }
        
        employee.FailedLoginCount = 0;
        employee.FirstFailedLoginAt = null;
        employee.LockoutEndAt = null;
        await AuditAsync(employee.Id, "LoginSucceeded");
        await unitOfWork.SaveChangesAsync();
        
        logger.LogInformation("Employee {EmployeeId} logged in", employee.Id);
        return ApiResponse.Success("Login successful.", GenerateJwt(employee));
    }

    public async Task<ApiResponse> SetupPasswordAsync(SetPasswordRequest request)
    {
        var token = await accountTokenRepository.GetUsableAsync(TokenHelper.Hash(request.Token), TokenPurpose.AccountSetup);
        if (token?.Employee == null)
        {
            return ApiResponse.Fail("The setup link is invalid or has expired.", 400);
        }
        
        var employee = token.Employee;
        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        employee.IsActive = true;
        employee.UpdatedAt = DateTime.UtcNow;
        token.UsedAt = DateTime.UtcNow;

        await AuditAsync(employee.Id, "AccountActivated");
        await notificationService.NotifyAsync(employee, "Your account is active", 
            "Your Employee Self-Service account has been activated. You can now log in.");
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Password set successful. Your account is now active and you can log in.");
    }

    public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var employee = await employeeRepository.GetByEmailWithRolesAsync(email);

        if (employee is { IsActive: true, PasswordHash: not null })
        {
            var token = await IssueTokenAsync(employee, TokenPurpose.PasswordReset,
                TimeSpan.FromMinutes(_authSettings.ResetTokenExpiryMinutes));
            
            var link = $"{_authSettings.ClientBaseUrl}/reset-password?token={token}";
            await notificationService.NotifyAsync(employee, ResetEmailTitle,
                "A password reset was requested for your account. Check your email for the link.",
                emailBody: MailUtils.PasswordReset(employee.FirstName, link, token, _authSettings.ResetTokenExpiryMinutes));
            await AuditAsync(employee.Id, "PasswordResetRequested");
            await unitOfWork.SaveChangesAsync();
        }
        
        return ApiResponse.Success("If the email is registered, a password reset link has been sent."); 
    }

    public async Task<ApiResponse> ResetPasswordAsync(SetPasswordRequest request)
    {
        var token = await accountTokenRepository.GetUsableAsync(TokenHelper.Hash(request.Token), TokenPurpose.PasswordReset);
        if (token?.Employee is not { IsActive: true })
            return ApiResponse.Fail("The reset link is invalid or has expired.", 400);

        var employee = token.Employee;
        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        employee.FailedLoginCount = 0;
        employee.FirstFailedLoginAt = null;
        employee.LockoutEndAt = null;
        employee.UpdatedAt = DateTime.UtcNow;
        token.UsedAt = DateTime.UtcNow;

        await AuditAsync(employee.Id, "PasswordReset");
        await notificationService.NotifyAsync(employee, "Your password was changed",
            "Your password was reset. If this wasn't you, contact HR immediately.");
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Password has been reset. You can now log in.");
    }

    public async Task<ApiResponse> ChangePasswordAsync(int employeeId, ChangePasswordRequest request)
    {
        var employee = await employeeRepository.GetByIdAsync(employeeId);
        if(employee == null) return ApiResponse.NotFound("Employee not found");
        
        if (employee.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, employee.PasswordHash))
            return ApiResponse.Fail("Current password is incorrect", 400);
        
        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        employee.UpdatedAt = DateTime.UtcNow;
        
        await AuditAsync(employee.Id, "PasswordChanged");
        await notificationService.NotifyAsync(employee, "Your password has been changed", 
            "Your password was changed. If you did not request this, contact HR immediately.");
        await unitOfWork.SaveChangesAsync();
        
        return ApiResponse.Success("Password changed successfully.");
    }

    public async Task IssueSetupLinkAsync(Employee employee)
    {
        var token = await IssueTokenAsync(employee, TokenPurpose.AccountSetup,
            TimeSpan.FromHours(_authSettings.SetupTokenExpiryHours));
        
        var link = $"{_authSettings.ClientBaseUrl}/setup-password?token={token}";
        await notificationService.NotifyAsync(employee, SetupEmailTitle,
            "Your Employee Self-Service account was created. Check your email to set your password.",
            emailBody: MailUtils.AccountSetup(employee.FirstName, employee.EmployeeNumber, link, token,
                _authSettings.SetupTokenExpiryHours));
    }
    
    private async Task<string> IssueTokenAsync(Employee employee, TokenPurpose purpose, TimeSpan lifetime)
    {
        if (employee.Id != 0)
        {
            await accountTokenRepository.InvalidateActiveAsync(employee.Id, purpose);

            // The old link is dead now, so an older email still waiting in the queue must not go out
            await emailQueueRepository.CancelPendingAsync(employee.Id,
                purpose == TokenPurpose.AccountSetup ? SetupEmailTitle : ResetEmailTitle,
                "Cancelled: a newer link was issued before this email was sent.");
        }

        var token = TokenHelper.GenerateToken();
        await accountTokenRepository.AddAsync(new AccountToken
        {
            Employee = employee,
            TokenHash = TokenHelper.Hash(token),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            CreatedAt = DateTime.UtcNow
        });

        return token;
    }
    
    private void RegisterFailedLogin(Employee employee, DateTime now)
    {
        var windowStart = now.AddMinutes(-_authSettings.FailedLoginWindowMinutes);

        if (employee.FirstFailedLoginAt == null || employee.FirstFailedLoginAt < windowStart)
        {
            employee.FirstFailedLoginAt = now;
            employee.FailedLoginCount = 1;
        }
        else
        {
            employee.FailedLoginCount++;
        }

        if (employee.FailedLoginCount >= _authSettings.MaxFailedLogins)
        {
            employee.LockoutEndAt = now.AddMinutes(_authSettings.LockoutMinutes);
            employee.FailedLoginCount = 0;
            employee.FirstFailedLoginAt = null;
        }
    }
    
    private Task AuditAsync(int? employeeId, string action, string? details = null) =>
        auditLogRepository.AddAsync(new AuditLog
        {
            EmployeeId = employeeId,
            Action = action,
            TargetEntity = nameof(Employee),
            TargetId = employeeId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    
    private LoginResponse GenerateJwt(Employee employee)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);
        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        var roles = employee.Roles.Select(r => r.Role).ToList();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, employee.Email),
            new("employeeNumber", employee.EmployeeNumber)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r.ToString())));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            Employee = new EmployeeSummaryResponse
            {
                Id = employee.Id,
                EmployeeNumber = employee.EmployeeNumber,
                FullName = fullName,
                Email = employee.Email,
                Roles = roles
            }
        };
    }

}