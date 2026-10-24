using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface ICourseAssignmentService
{
    Task<ApiResponse> GetMineAsync(int employeeId, CourseAssignmentQuery query);
    Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int assignmentId);
    Task<ApiResponse> CompleteLessonAsync(int employeeId, int assignmentId, int lessonId);
    Task<ApiResponse> SubmitAttemptAsync(int employeeId, int assignmentId, SubmitAttemptRequest request);
    Task<ApiResponse> ResetAsync(int hrId, int assignmentId, ReasonRequest request);
}
