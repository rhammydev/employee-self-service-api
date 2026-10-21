using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Employee;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class EmployeeService(
    IEmployeeRepository employeeRepository,
    IOrganizationRepository organizationRepository,
    IAccountTokenRepository accountTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork) : IEmployeeService
{
    public async Task<ApiResponse> CreateAsync(int actorId, CreateEmployeeRequest request)
    {
        var email =  request.Email.Trim().ToLowerInvariant();
        var employeeNumber = request.EmployeeNumber.Trim().ToUpperInvariant();
        
        if (await employeeRepository.EmailExistsAsync(email))
            return ApiResponse.Conflict("An employee with this email already exists");
        if (await employeeRepository.EmployeeNumberExistsAsync(employeeNumber))
            return ApiResponse.Conflict("An employee with this number already exists");
        
        var orgError = await ValidateDepartmentAndGradeAsync(request.DepartmentId, request.GradeId);
        if (orgError != null) return orgError;

        if (request.ReportingManagerId.HasValue)
        {
            var managerError = await ValidateManagerAsync(request.ReportingManagerId.Value);
            if (managerError != null) return managerError;
        }
        
        var now = DateTime.UtcNow;
        var employee = new Employee
        {
            EmployeeNumber = employeeNumber,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber?.Trim(),
            DepartmentId = request.DepartmentId,
            GradeId = request.GradeId,
            ReportingManagerId = request.ReportingManagerId,
            IsActive = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        
        employee.Roles.Add(new EmployeeRole
        {
            Role = UserRole.Staff,
            AssignedById = actorId,
            AssignedAt = now
        });
        
        await employeeRepository.AddAsync(employee);
        await authService.IssueSetupLinkAsync(employee);
        await AuditAsync(actorId, "EmployeeCreated", null, $"Created {employeeNumber}");
        await unitOfWork.SaveChangesAsync();

        var result = await employeeRepository.GetDetailsAsync(employee.Id);
        return ApiResponse.Success("Employee created, A setup link has been emailed", ToProfile(result!), 201);
    }

    public async Task<ApiResponse> ResendSetupLinkAsync(int actorId, int employeeId)
    {
        var employee = await employeeRepository.GetByIdWithRolesAsync(employeeId);
        if (employee == null)
            return ApiResponse.NotFound("Employee does not exist");
        if (employee.PasswordHash != null)
            return ApiResponse.Conflict("This employee has already completed account setup");
        
        await authService.IssueSetupLinkAsync(employee);
        await AuditAsync(actorId, "SetupLinkResent", employee.Id);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("A new setup link has been emailed. The previous link is now invalid.");
    }

    public async Task<ApiResponse> UpdateRolesAsync(int actorId, int employeeId, UpdateRolesRequest request)
    {
        var employee = await employeeRepository.GetByIdWithRolesAsync(employeeId);
        if (employee == null)
            return ApiResponse.NotFound("Employee not found.");

        var desired = request.Roles.Append(UserRole.Staff).ToHashSet();   

        if (employeeId == actorId && !desired.Contains(UserRole.Admin))
            return ApiResponse.Fail("You cannot remove your own Admin role.", 400);

        var removingManager = employee.Roles.Any(r => r.Role == UserRole.Manager) && !desired.Contains(UserRole.Manager);
        if (removingManager && await employeeRepository.HasDirectReportsAsync(employeeId))
            return ApiResponse.Conflict("This employee still has direct reports. Reassign them before removing the Manager role.");

        var now = DateTime.UtcNow;
        foreach (var role in employee.Roles.Where(r => !desired.Contains(r.Role)).ToList())
            employeeRepository.RemoveRole(role);

        foreach (var role in desired.Where(d => employee.Roles.All(r => r.Role != d)))
            employee.Roles.Add(new EmployeeRole { Role = role, AssignedById = actorId, AssignedAt = now });

        employee.UpdatedAt = now;
        await AuditAsync(actorId, "RolesUpdated", employeeId, string.Join(",", desired.OrderBy(r => r)));
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Roles updated. They take effect on the employee's next request.",
            new { EmployeeId = employeeId, Roles = desired.OrderBy(r => r).ToList() });
    }

    public async Task<ApiResponse> UpdateEmploymentAsync(int actorId, int employeeId, UpdateEmploymentRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse> UpdateStatusAsync(int actorId, int employeeId, UpdateStatusRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse> GetAllAsync(EmployeeQuery query)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse> GetByIdAsync(int employeeId)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse> GetMyProfileAsync(int employeeId)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse> UpdateMyPhoneAsync(int employeeId, UpdatePhoneRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse> GetMyTeamAsync(int managerId, PagedRequest request)
    {
        throw new NotImplementedException();
    }
    
    private async Task<ApiResponse?> ValidateDepartmentAndGradeAsync(int departmentId, int gradeId)
    {
        if (!await organizationRepository.DepartmentExistsAsync(departmentId))
            return ApiResponse.Fail("Department does not exist.", 400, ResponseCodes.BadRequest,
                [new ApiError("departmentId", "Department does not exist.")]);

        if (!await organizationRepository.GradeExistsAsync(gradeId))
            return ApiResponse.Fail("Grade does not exist.", 400, ResponseCodes.BadRequest,
                [new ApiError("gradeId", "Grade does not exist.")]);

        return null;
    }
    
    private async Task<ApiResponse?> ValidateManagerAsync(int managerId)
    {
        var manager = await employeeRepository.GetByIdWithRolesAsync(managerId);

        if (manager == null)
            return ApiResponse.Fail("Reporting manager does not exist.", 400);
        if (!manager.IsActive)
            return ApiResponse.Fail("Reporting manager is not active.", 400);
        if (manager.Roles.All(r => r.Role != UserRole.Manager))
            return ApiResponse.Fail("Reporting manager must hold the Manager role.", 400);

        return null;
    }
    
    private Task AuditAsync(int actorId, string action, int? targetId, string? details = null) =>
        auditLogRepository.AddAsync(new AuditLog
        {
            EmployeeId = actorId,
            Action = action,
            TargetEntity = nameof(Employee),
            TargetId = targetId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    
    private static EmployeeProfileResponse ToProfile(Employee e) => new()
    {
        Id = e.Id,
        EmployeeNumber = e.EmployeeNumber,
        FirstName = e.FirstName,
        LastName = e.LastName,
        FullName = $"{e.FirstName} {e.LastName}",
        Email = e.Email,
        PhoneNumber = e.PhoneNumber,
        IsActive = e.IsActive,
        HasCompletedSetup = e.PasswordHash != null,
        DepartmentId = e.DepartmentId,
        DepartmentName = e.Department?.Name ?? string.Empty,
        GradeId = e.GradeId,
        GradeCode = e.Grade?.Code ?? string.Empty,
        GradeName = e.Grade?.Name ?? string.Empty,
        ReportingManagerId = e.ReportingManagerId,
        ReportingManagerName = e.ReportingManager == null
            ? null
            : $"{e.ReportingManager.FirstName} {e.ReportingManager.LastName}",
        Roles = e.Roles.Select(r => r.Role).OrderBy(r => r).ToList(),
        CreatedAt = e.CreatedAt
    };
    
    private async Task<bool> CreatesCycleAsync(int employeeId, int managerId)
    {
        var visited = new HashSet<int>();
        int? current = managerId;

        while (current.HasValue && visited.Add(current.Value))
        {
            if (current.Value == employeeId) return true;
            current = await employeeRepository.GetReportingManagerIdAsync(current.Value);
        }

        return false;
    }
    
    private static PagedResult<EmployeeProfileResponse> Map(PagedResult<Employee> page) =>
        new() { Items = page.Items.Select(ToProfile).ToList(), Pagination = page.Pagination };
}