using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface ICourseService
{
    Task<ApiResponse> CreateAsync(int hrId, SaveCourseRequest request);
    Task<ApiResponse> UpdateAsync(int courseId, SaveCourseRequest request);
    Task<ApiResponse> GetAllAsync(CourseQuery query);
    Task<ApiResponse> GetByIdAsync(int courseId);

    Task<ApiResponse> AddLessonAsync(int courseId, SaveLessonRequest request);
    Task<ApiResponse> UpdateLessonAsync(int courseId, int lessonId, SaveLessonRequest request);
    Task<ApiResponse> DeleteLessonAsync(int courseId, int lessonId);
    Task<ApiResponse> ReorderLessonsAsync(int courseId, ReorderRequest request);

    Task<ApiResponse> AddQuestionAsync(int courseId, SaveQuestionRequest request);
    Task<ApiResponse> UpdateQuestionAsync(int courseId, int questionId, SaveQuestionRequest request);
    Task<ApiResponse> DeleteQuestionAsync(int courseId, int questionId);
    Task<ApiResponse> ReorderQuestionsAsync(int courseId, ReorderRequest request);

    Task<ApiResponse> PublishAsync(int courseId);
    Task<ApiResponse> AssignAsync(int hrId, int courseId, AssignCourseRequest request);
    Task<ApiResponse> GetAssignmentsAsync(int courseId, CourseAssignmentQuery query);
}
