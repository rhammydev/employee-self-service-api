using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class CourseAssignmentRepository(AppDbContext context) : ICourseAssignmentRepository
{
    public async Task AddAsync(CourseAssignment assignment) =>
        await context.CourseAssignments.AddAsync(assignment);

    public async Task<HashSet<int>> GetAssignedEmployeeIdsAsync(int courseId, IReadOnlyCollection<int> employeeIds) =>
        (await context.CourseAssignments
            .Where(a => a.CourseId == courseId && employeeIds.Contains(a.EmployeeId))
            .Select(a => a.EmployeeId)
            .ToListAsync())
        .ToHashSet();

    public Task<CourseAssignment?> GetForLearnerAsync(int id) =>
        context.CourseAssignments
            .AsSplitQuery()
            .Include(a => a.Employee)
            .Include(a => a.Course).ThenInclude(c => c!.Lessons)
            .Include(a => a.Course).ThenInclude(c => c!.Questions).ThenInclude(q => q.Options)
            .Include(a => a.LessonProgress)
            .Include(a => a.Attempts)
            .Include(a => a.Resets).ThenInclude(r => r.ResetBy)
            .FirstOrDefaultAsync(a => a.Id == id);

    public Task<PagedResult<CourseAssignment>> GetPagedForEmployeeAsync(int employeeId, CourseAssignmentQuery query, DateOnly today) =>
        Filter(WithSummaryData().Where(a => a.EmployeeId == employeeId), query, today)
            .OrderBy(a => a.DueDate).ThenBy(a => a.Id)
            .ToPagedResultAsync(query);

    public Task<PagedResult<CourseAssignment>> GetPagedForCourseAsync(int courseId, CourseAssignmentQuery query, DateOnly today) =>
        Filter(WithSummaryData().Where(a => a.CourseId == courseId), query, today)
            .OrderBy(a => a.Employee!.LastName).ThenBy(a => a.Employee!.FirstName)
            .ToPagedResultAsync(query);

    private IQueryable<CourseAssignment> WithSummaryData() =>
        context.CourseAssignments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Employee)
            .Include(a => a.Course).ThenInclude(c => c!.Lessons)
            .Include(a => a.LessonProgress)
            .Include(a => a.Attempts);

    private static IQueryable<CourseAssignment> Filter(IQueryable<CourseAssignment> assignments, CourseAssignmentQuery query, DateOnly today)
    {
        if (query.Status.HasValue)
            assignments = assignments.Where(a => a.Status == query.Status);

        if (query.Overdue == true)
            assignments = assignments.Where(a => a.DueDate < today && a.Status != AssignmentStatus.Completed);
        else if (query.Overdue == false)
            assignments = assignments.Where(a => a.DueDate >= today || a.Status == AssignmentStatus.Completed);

        return assignments;
    }
}
