using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface ICourseRepository
{
    Task AddAsync(Course course);

    Task<Course?> GetForUpdateAsync(int id);

    Task<Course?> GetDetailsAsync(int id);

    Task<PagedResult<Course>> GetPagedAsync(CourseQuery query);

    void RemoveLesson(Lesson lesson);
    void RemoveQuestion(Question question);
    void RemoveOption(QuestionOption option);
}
