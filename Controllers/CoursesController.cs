using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/courses")]
[Authorize(Roles = nameof(UserRole.HR))]
public class CoursesController(ICourseService courses) : ControllerBase
{
    private ObjectResult Respond(ApiResponse response) => StatusCode(response.HttpStatusCode, response);

    [HttpPost]
    public async Task<IActionResult> Create(SaveCourseRequest request) =>
        Respond(await courses.CreateAsync(User.GetEmployeeId(), request));

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CourseQuery query) =>
        Respond(await courses.GetAllAsync(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        Respond(await courses.GetByIdAsync(id));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveCourseRequest request) =>
        Respond(await courses.UpdateAsync(id, request));

    [HttpPost("{id:int}/lessons")]
    public async Task<IActionResult> AddLesson(int id, SaveLessonRequest request) =>
        Respond(await courses.AddLessonAsync(id, request));

    [HttpPut("{id:int}/lessons/{lessonId:int}")]
    public async Task<IActionResult> UpdateLesson(int id, int lessonId, SaveLessonRequest request) =>
        Respond(await courses.UpdateLessonAsync(id, lessonId, request));

    [HttpDelete("{id:int}/lessons/{lessonId:int}")]
    public async Task<IActionResult> DeleteLesson(int id, int lessonId) =>
        Respond(await courses.DeleteLessonAsync(id, lessonId));

    [HttpPut("{id:int}/lessons/order")]
    public async Task<IActionResult> ReorderLessons(int id, ReorderRequest request) =>
        Respond(await courses.ReorderLessonsAsync(id, request));

    [HttpPost("{id:int}/questions")]
    public async Task<IActionResult> AddQuestion(int id, SaveQuestionRequest request) =>
        Respond(await courses.AddQuestionAsync(id, request));

    [HttpPut("{id:int}/questions/{questionId:int}")]
    public async Task<IActionResult> UpdateQuestion(int id, int questionId, SaveQuestionRequest request) =>
        Respond(await courses.UpdateQuestionAsync(id, questionId, request));

    [HttpDelete("{id:int}/questions/{questionId:int}")]
    public async Task<IActionResult> DeleteQuestion(int id, int questionId) =>
        Respond(await courses.DeleteQuestionAsync(id, questionId));

    [HttpPut("{id:int}/questions/order")]
    public async Task<IActionResult> ReorderQuestions(int id, ReorderRequest request) =>
        Respond(await courses.ReorderQuestionsAsync(id, request));

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id) =>
        Respond(await courses.PublishAsync(id));

    [HttpPost("{id:int}/assignments")]
    public async Task<IActionResult> Assign(int id, AssignCourseRequest request) =>
        Respond(await courses.AssignAsync(User.GetEmployeeId(), id, request));

    [HttpGet("{id:int}/assignments")]
    public async Task<IActionResult> GetAssignments(int id, [FromQuery] CourseAssignmentQuery query) =>
        Respond(await courses.GetAssignmentsAsync(id, query));
}
