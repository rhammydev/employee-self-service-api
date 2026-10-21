using System.Security.Claims;
using System.Text;
using EmployeeSelfServiceAPI.Domain.Settings;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeSelfServiceAPI.Extensions;

public static class AuthenticationExtensions
{
     public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");
        var jwtSettings = section.Get<JwtSettings>() ?? new JwtSettings();

        if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32 ||
            string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
            string.IsNullOrWhiteSpace(jwtSettings.Audience))
        {
            throw new InvalidOperationException(
                "JWT is not configured. Set Jwt:Key (at least 32 bytes), Jwt:Issuer and Jwt:Audience.");
        }

        services.Configure<JwtSettings>(section);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var idClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!int.TryParse(idClaim, out var employeeId))
                        {
                            context.Fail("Token does not identify an employee.");
                            return;
                        }

                        var employees = context.HttpContext.RequestServices.GetRequiredService<IEmployeeRepository>();
                        var roles = await employees.GetActiveRolesAsync(employeeId);
                        if (roles is null)
                        {
                            context.Fail("Employee account is inactive or no longer exists.");
                            return;
                        }

                        var identity = (ClaimsIdentity)context.Principal!.Identity!;
                        foreach (var claim in identity.FindAll(ClaimTypes.Role).ToList())
                            identity.RemoveClaim(claim);
                        identity.AddClaims(roles.Select(r => new Claim(ClaimTypes.Role, r.ToString())));
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(
                            "Authentication is required. Provide a valid bearer token.",
                            401, ResponseCodes.Unauthorized));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(
                            "You do not have permission to access this resource.",
                            403, ResponseCodes.Forbidden));
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}