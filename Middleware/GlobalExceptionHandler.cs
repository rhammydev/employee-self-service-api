using EmployeeSelfServiceAPI.DTOs.Generic;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var response = exception switch
        {
            // RowVersion mismatch: someone else changed the record first (e.g. two approvals at once)
            DbUpdateConcurrencyException =>
                ApiResponse.Conflict("This record was changed by another request. Reload it and try again."),

            // 2601/2627 = unique index / constraint violation
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                ApiResponse.Conflict("A record with the same details already exists."),

            _ => null
        };

        if (response is null)
        {
            // Log the exception, never the request body (could contain passwords or payroll data)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
            response = ApiResponse.Fail("An unexpected error occurred.", 500, ResponseCodes.ServerError);
        }

        httpContext.Response.StatusCode = response.HttpStatusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}