using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;
using static EmployeeSelfServiceAPI.Mappings.LearningMappings;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class CourseService(
    ICourseRepository courseRepository,
    ICourseAssignmentRepository assignmentRepository,
    IEmployeeRepository employeeRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IAppClock clock) : ICourseService
{
    private const string CourseNotFound = "Course not found.";

    public async Task<ApiResponse> CreateAsync(int hrId, SaveCourseRequest request)
    {
        var now = clock.UtcNow;
        var course = new Course
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            PassMark = request.PassMark,
            Status = CourseStatus.Draft,
            CreatedById = hrId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await courseRepository.AddAsync(course);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Course created as draft.", new { course.Id, course.Status }, 201);
    }

    public async Task<ApiResponse> UpdateAsync(int courseId, SaveCourseRequest request)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        course!.Title = request.Title.Trim();
        course.Description = request.Description?.Trim();
        course.PassMark = request.PassMark;
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Course updated.");
    }

    public async Task<ApiResponse> GetAllAsync(CourseQuery query)
    {
        var page = await courseRepository.GetPagedAsync(query);
        return ApiResponse.Paged("Courses retrieved.", new PagedResult<CourseSummaryResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(ToCourseSummary).ToList()
        });
    }

    public async Task<ApiResponse> GetByIdAsync(int courseId)
    {
        var course = await courseRepository.GetDetailsAsync(courseId);
        return course == null
            ? ApiResponse.NotFound(CourseNotFound)
            : ApiResponse.Success("Course retrieved.", ToCourseDetail(course));
    }

    // ---- Lessons (LMS-01.3: editable only while Draft) ----

    public async Task<ApiResponse> AddLessonAsync(int courseId, SaveLessonRequest request)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        var lesson = new Lesson { SortOrder = course!.Lessons.Count == 0 ? 1 : course.Lessons.Max(l => l.SortOrder) + 1 };
        Apply(lesson, request);
        course.Lessons.Add(lesson);
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Lesson added.", new { lesson.Id, lesson.SortOrder }, 201);
    }

    public async Task<ApiResponse> UpdateLessonAsync(int courseId, int lessonId, SaveLessonRequest request)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        var lesson = course!.Lessons.FirstOrDefault(l => l.Id == lessonId);
        if (lesson == null)
            return ApiResponse.NotFound("Lesson not found in this course.");

        Apply(lesson, request);
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Lesson updated.");
    }

    public async Task<ApiResponse> DeleteLessonAsync(int courseId, int lessonId)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        var lesson = course!.Lessons.FirstOrDefault(l => l.Id == lessonId);
        if (lesson == null)
            return ApiResponse.NotFound("Lesson not found in this course.");

        courseRepository.RemoveLesson(lesson);
        Renumber(course.Lessons.Where(l => l.Id != lessonId).OrderBy(l => l.SortOrder), (l, i) => l.SortOrder = i);
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Lesson removed.");
    }

    public async Task<ApiResponse> ReorderLessonsAsync(int courseId, ReorderRequest request)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        if (!SameIds(course!.Lessons.Select(l => l.Id), request.Ids))
            return ApiResponse.Fail("Provide every lesson id of this course exactly once.", 400);

        var byId = course.Lessons.ToDictionary(l => l.Id);
        Renumber(request.Ids.Select(id => byId[id]), (l, i) => l.SortOrder = i);
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Lessons reordered.");
    }

    // ---- Questions ----

    public async Task<ApiResponse> AddQuestionAsync(int courseId, SaveQuestionRequest request)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        var question = new Question
        {
            Text = request.Text.Trim(),
            SortOrder = course!.Questions.Count == 0 ? 1 : course.Questions.Max(q => q.SortOrder) + 1
        };
        AddOptions(question, request.Options);
        course.Questions.Add(question);
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Question added.", new { question.Id, question.SortOrder }, 201);
    }

    public async Task<ApiResponse> UpdateQuestionAsync(int courseId, int questionId, SaveQuestionRequest request)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        var question = course!.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
            return ApiResponse.NotFound("Question not found in this course.");

        // Options are replaced as a set. Safe: a Draft course has never been attempted.
        question.Text = request.Text.Trim();
        foreach (var option in question.Options.ToList())
        {
            courseRepository.RemoveOption(option);
            question.Options.Remove(option);
        }
        AddOptions(question, request.Options);
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Question updated.");
    }

    public async Task<ApiResponse> DeleteQuestionAsync(int courseId, int questionId)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        var question = course!.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
            return ApiResponse.NotFound("Question not found in this course.");

        courseRepository.RemoveQuestion(question);
        Renumber(course.Questions.Where(q => q.Id != questionId).OrderBy(q => q.SortOrder), (q, i) => q.SortOrder = i);
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Question removed.");
    }

    public async Task<ApiResponse> ReorderQuestionsAsync(int courseId, ReorderRequest request)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        if (!SameIds(course!.Questions.Select(q => q.Id), request.Ids))
            return ApiResponse.Fail("Provide every question id of this course exactly once.", 400);

        var byId = course.Questions.ToDictionary(q => q.Id);
        Renumber(request.Ids.Select(id => byId[id]), (q, i) => q.SortOrder = i);
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Questions reordered.");
    }

    // ---- Publish & assign ----

    public async Task<ApiResponse> PublishAsync(int courseId)
    {
        var (course, error) = await LoadDraftAsync(courseId);
        if (error != null) return error;

        // LMS-01.4
        if (course!.Lessons.Count == 0 || course.Questions.Count == 0)
            return ApiResponse.Fail("A course needs at least one lesson and one question before it can be published.", 400);
        if (course.Questions.Any(q => q.Options.Count(o => o.IsCorrect) != 1 || q.Options.Count < 2))
            return ApiResponse.Fail("Every question needs at least two options and exactly one correct option.", 400);

        course.Status = CourseStatus.Published;
        course.PublishedAt = clock.UtcNow;
        course.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Course published. It can no longer be edited; create a new course for a new version.");
    }

    public async Task<ApiResponse> AssignAsync(int hrId, int courseId, AssignCourseRequest request)
    {
        var course = await courseRepository.GetDetailsAsync(courseId);
        if (course == null)
            return ApiResponse.NotFound(CourseNotFound);
        if (course.Status != CourseStatus.Published)
            return ApiResponse.Conflict("Only published courses can be assigned.");   // LMS-02.1
        if (request.DueDate < clock.Today)
            return ApiResponse.Fail("The due date cannot be in the past.", 400, ResponseCodes.BadRequest,
                [new ApiError("dueDate", "The due date cannot be in the past.")]);

        var employeeIds = request.EmployeeIds.Distinct().ToList();
        var employees = (await employeeRepository.GetByIdsAsync(employeeIds)).ToDictionary(e => e.Id);
        var alreadyAssigned = await assignmentRepository.GetAssignedEmployeeIdsAsync(courseId, employeeIds);

        var now = clock.UtcNow;
        var results = new List<AssignResultItem>();
        var created = new List<(AssignResultItem Item, CourseAssignment Assignment)>();

        foreach (var employeeId in employeeIds)
        {
            var item = new AssignResultItem { EmployeeId = employeeId };
            results.Add(item);

            if (!employees.TryGetValue(employeeId, out var employee))
            {
                (item.StatusCode, item.Message) = (404, "Employee not found.");
                continue;
            }
            if (!employee.IsActive)
            {
                (item.StatusCode, item.Message) = (400, "Employee is not active.");
                continue;
            }
            if (alreadyAssigned.Contains(employeeId))
            {
                (item.StatusCode, item.Message) = (409, "This course is already assigned to this employee.");
                continue;
            }

            var assignment = new CourseAssignment
            {
                CourseId = courseId,
                Employee = employee,
                AssignedById = hrId,
                DueDate = request.DueDate,
                Status = AssignmentStatus.NotStarted,
                CurrentCycle = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            await assignmentRepository.AddAsync(assignment);
            created.Add((item, assignment));

            await notificationService.NotifyAsync(employee, "New course assigned",
                $"You have been assigned \"{course.Title}\". Complete it by {request.DueDate:dd MMM yyyy}.",
                relatedEntity: nameof(Course), relatedEntityId: courseId);
        }

        if (created.Count > 0)
            await unitOfWork.SaveChangesAsync();

        foreach (var (item, assignment) in created)
            (item.Assigned, item.AssignmentId, item.StatusCode, item.Message) = (true, assignment.Id, 201, "Assigned.");

        var response = new AssignResultResponse { Results = results };
        return ApiResponse.Success($"{response.AssignedCount} of {results.Count} employee(s) assigned.", response);
    }

    public async Task<ApiResponse> GetAssignmentsAsync(int courseId, CourseAssignmentQuery query)
    {
        var page = await assignmentRepository.GetPagedForCourseAsync(courseId, query, clock.Today);
        return ApiResponse.Paged("Assignments retrieved.", new PagedResult<AssignmentSummaryResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(a => ToAssignmentSummary(a, clock.Today)).ToList()
        });
    }

    // ---- helpers ----

    private async Task<(Course? Course, ApiResponse? Error)> LoadDraftAsync(int courseId)
    {
        var course = await courseRepository.GetForUpdateAsync(courseId);
        if (course == null)
            return (null, ApiResponse.NotFound(CourseNotFound));
        if (course.Status != CourseStatus.Draft)
            return (null, ApiResponse.Conflict("A published course cannot be changed. Create a new course for a new version."));
        return (course, null);
    }

    private static void Apply(Lesson lesson, SaveLessonRequest request)
    {
        lesson.Title = request.Title.Trim();
        lesson.Content = request.Content?.Trim();
        lesson.MaterialUrl = request.MaterialUrl?.Trim();
        lesson.VideoUrl = request.VideoUrl?.Trim();
    }

    private static void AddOptions(Question question, List<SaveOptionRequest> options)
    {
        var order = 1;
        foreach (var option in options)
            question.Options.Add(new QuestionOption { Text = option.Text.Trim(), IsCorrect = option.IsCorrect, SortOrder = order++ });
    }

    private static void Renumber<T>(IEnumerable<T> items, Action<T, int> setOrder)
    {
        var order = 1;
        foreach (var item in items)
            setOrder(item, order++);
    }

    private static bool SameIds(IEnumerable<int> existing, List<int> requested) =>
        existing.ToHashSet().SetEquals(requested) && requested.Count == requested.Distinct().Count();
}
