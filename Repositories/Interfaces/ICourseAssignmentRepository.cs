using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface ICourseAssignmentRepository
{
    Task AddAsync(CourseAssignment assignment);

    Task<HashSet<int>> GetAssignedEmployeeIdsAsync(int courseId, IReadOnlyCollection<int> employeeIds);

    Task<CourseAssignment?> GetForLearnerAsync(int id);

    Task<PagedResult<CourseAssignment>> GetPagedForEmployeeAsync(int employeeId, CourseAssignmentQuery query, DateOnly today);
    Task<PagedResult<CourseAssignment>> GetPagedForCourseAsync(int courseId, CourseAssignmentQuery query, DateOnly today);
}
