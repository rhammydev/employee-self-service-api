using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class CourseRepository(AppDbContext context) : ICourseRepository
{
    public async Task AddAsync(Course course) =>
        await context.Courses.AddAsync(course);

    public Task<Course?> GetForUpdateAsync(int id) =>
        context.Courses
            .AsSplitQuery()
            .Include(c => c.Lessons)
            .Include(c => c.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<Course?> GetDetailsAsync(int id) =>
        context.Courses
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.CreatedBy)
            .Include(c => c.Lessons)
            .Include(c => c.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<PagedResult<Course>> GetPagedAsync(CourseQuery query)
    {
        var courses = context.Courses
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Lessons)
            .Include(c => c.Questions)
            .AsQueryable();

        if (query.Status.HasValue)
            courses = courses.Where(c => c.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.Search))
            courses = courses.Where(c => c.Title.Contains(query.Search.Trim()));

        return courses.OrderByDescending(c => c.CreatedAt).ToPagedResultAsync(query);
    }

    public void RemoveLesson(Lesson lesson) => context.Lessons.Remove(lesson);
    public void RemoveQuestion(Question question) => context.Questions.Remove(question);
    public void RemoveOption(QuestionOption option) => context.QuestionOptions.Remove(option);
}
