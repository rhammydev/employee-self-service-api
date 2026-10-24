using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Learning;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;
using static EmployeeSelfServiceAPI.Mappings.LearningMappings;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class CourseAssignmentService(
    ICourseAssignmentRepository assignmentRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IAppClock clock) : ICourseAssignmentService
{
    private const string NotFoundMessage = "Course assignment not found.";
    private const string NotYoursMessage = "This course assignment belongs to another employee.";

    public async Task<ApiResponse> GetMineAsync(int employeeId, CourseAssignmentQuery query)
    {
        var page = await assignmentRepository.GetPagedForEmployeeAsync(employeeId, query, clock.Today);
        return ApiResponse.Paged("Course assignments retrieved.", new PagedResult<AssignmentSummaryResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(a => ToAssignmentSummary(a, clock.Today)).ToList()
        });
    }

    public async Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int assignmentId)
    {
        var assignment = await assignmentRepository.GetForLearnerAsync(assignmentId);
        if (assignment == null)
            return ApiResponse.NotFound(NotFoundMessage);

        if (assignment.EmployeeId != userId && !roles.Contains(UserRole.HR))
            return ApiResponse.Forbidden(NotYoursMessage);

        return ApiResponse.Success("Course assignment retrieved.", ToAssignmentDetail(assignment, clock.Today));
    }

    public async Task<ApiResponse> CompleteLessonAsync(int employeeId, int assignmentId, int lessonId)
    {
        var assignment = await assignmentRepository.GetForLearnerAsync(assignmentId);
        if (assignment == null)
            return ApiResponse.NotFound(NotFoundMessage);
        if (assignment.EmployeeId != employeeId)
            return ApiResponse.Forbidden(NotYoursMessage);
        if (assignment.Course!.Lessons.All(l => l.Id != lessonId))
            return ApiResponse.NotFound("Lesson not found in this course.");

        if (assignment.LessonProgress.Any(p => p.LessonId == lessonId))
            return ApiResponse.Success("Lesson already completed.", ProgressOf(assignment));

        assignment.LessonProgress.Add(new LessonProgress { LessonId = lessonId, CompletedAt = clock.UtcNow });
        if (assignment.Status == AssignmentStatus.NotStarted)
            assignment.Status = AssignmentStatus.InProgress;
        assignment.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Lesson completed.", ProgressOf(assignment));
    }

    public async Task<ApiResponse> SubmitAttemptAsync(int employeeId, int assignmentId, SubmitAttemptRequest request)
    {
        var assignment = await assignmentRepository.GetForLearnerAsync(assignmentId);
        if (assignment == null)
            return ApiResponse.NotFound(NotFoundMessage);
        if (assignment.EmployeeId != employeeId)
            return ApiResponse.Forbidden(NotYoursMessage);

        if (assignment.Status == AssignmentStatus.Completed)
            return ApiResponse.Conflict("You have already passed this course.");
        if (assignment.Status == AssignmentStatus.AttemptsExhausted)
            return ApiResponse.Conflict("All attempts for this cycle are used. Ask HR to reset the assignment.");

        var course = assignment.Course!;

        var completed = assignment.LessonProgress.Select(p => p.LessonId).ToHashSet();
        if (course.Lessons.Any(l => !completed.Contains(l.Id)))
            return ApiResponse.Conflict("Complete every lesson before taking the assessment.");

        var questions = course.Questions.ToDictionary(q => q.Id);
        var errors = ValidateAnswers(request.Answers, questions);
        if (errors.Count > 0)
            return ApiResponse.Fail("The submission is invalid. No attempt was used.", 400, ResponseCodes.BadRequest, errors);

        var attemptNumber = assignment.Attempts.Count(a => a.CycleNumber == assignment.CurrentCycle) + 1;
        if (attemptNumber > AssessmentScorer.MaxAttemptsPerCycle)
            return ApiResponse.Conflict("All attempts for this cycle are used. Ask HR to reset the assignment.");

        var answers = request.Answers.Select(a => new AttemptAnswer
        {
            QuestionId = a.QuestionId,
            SelectedOptionId = a.SelectedOptionId,
            IsCorrect = questions[a.QuestionId].Options.Single(o => o.Id == a.SelectedOptionId).IsCorrect
        }).ToList();
        var correctCount = answers.Count(a => a.IsCorrect);
        var score = AssessmentScorer.Score(correctCount, questions.Count);
        var passed = AssessmentScorer.IsPass(score, course.PassMark);
        var now = clock.UtcNow;

        var attempt = new AssessmentAttempt
        {
            CycleNumber = assignment.CurrentCycle,
            AttemptNumber = attemptNumber,
            CorrectCount = correctCount,
            TotalQuestions = questions.Count,
            Score = score,
            IsPassed = passed,
            SubmittedAt = now,
            Answers = answers
        };
        assignment.Attempts.Add(attempt);

        if (passed)
        {
            assignment.Status = AssignmentStatus.Completed;
            assignment.CompletedAt = now;
        }
        else
        {
            assignment.Status = attemptNumber >= AssessmentScorer.MaxAttemptsPerCycle
                ? AssignmentStatus.AttemptsExhausted
                : AssignmentStatus.InProgress;
        }
        assignment.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync();

        var result = new AttemptResultResponse
        {
            CycleNumber = attempt.CycleNumber,
            AttemptNumber = attempt.AttemptNumber,
            CorrectCount = correctCount,
            TotalQuestions = questions.Count,
            Score = Math.Round(score, 2, MidpointRounding.AwayFromZero),
            IsPassed = passed,
            SubmittedAt = now,
            AssignmentStatus = assignment.Status,
            AttemptsRemaining = assignment.Status == AssignmentStatus.InProgress
                ? AssessmentScorer.MaxAttemptsPerCycle - attemptNumber
                : 0,
            ClientScoreIgnored = request.Score.HasValue
        };

        return ApiResponse.Success(passed
            ? "Passed. The course is complete."
            : assignment.Status == AssignmentStatus.AttemptsExhausted
                ? "Not passed. All attempts for this cycle are used."
                : "Not passed. You can try again.", result);
    }

    public async Task<ApiResponse> ResetAsync(int hrId, int assignmentId, ReasonRequest request)
    {
        var assignment = await assignmentRepository.GetForLearnerAsync(assignmentId);
        if (assignment == null)
            return ApiResponse.NotFound(NotFoundMessage);

        if (assignment.Status != AssignmentStatus.AttemptsExhausted)
            return ApiResponse.Conflict(assignment.Status == AssignmentStatus.Completed
                ? "A completed assignment cannot be reset."
                : "Only assignments with all attempts used can be reset.");

        var fromCycle = assignment.CurrentCycle;
        assignment.CurrentCycle++;
        assignment.Status = AssignmentStatus.InProgress;   // lesson progress is kept
        assignment.UpdatedAt = clock.UtcNow;
        assignment.Resets.Add(new AssignmentReset
        {
            FromCycle = fromCycle,
            ToCycle = assignment.CurrentCycle,
            Reason = request.Reason.Trim(),
            ResetById = hrId,
            ResetAt = clock.UtcNow
        });

        await notificationService.NotifyAsync(assignment.Employee!, "Course attempts reset",
            $"HR reset your attempts for \"{assignment.Course!.Title}\". You have {AssessmentScorer.MaxAttemptsPerCycle} new attempts.",
            relatedEntity: nameof(CourseAssignment), relatedEntityId: assignment.Id);
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success($"Assignment reset. Cycle {assignment.CurrentCycle} started; earlier attempts remain visible.");
    }

    // ---- helpers ----

    private static List<ApiError> ValidateAnswers(List<AttemptAnswerRequest> answers, Dictionary<int, Question> questions)
    {
        var errors = new List<ApiError>();

        for (var i = 0; i < answers.Count; i++)
        {
            var answer = answers[i];
            if (!questions.TryGetValue(answer.QuestionId, out var question))
                errors.Add(new ApiError($"answers[{i}].questionId", "This question is not part of the course."));
            else if (question.Options.All(o => o.Id != answer.SelectedOptionId))
                errors.Add(new ApiError($"answers[{i}].selectedOptionId", "This option does not belong to the question."));
        }

        if (answers.Select(a => a.QuestionId).Distinct().Count() != answers.Count)
            errors.Add(new ApiError("answers", "Each question can be answered only once."));

        var missing = questions.Keys.Except(answers.Select(a => a.QuestionId)).ToList();
        if (missing.Count > 0)
            errors.Add(new ApiError("answers", $"Every question must be answered. Missing: {string.Join(", ", missing)}."));

        return errors;
    }

    private static object ProgressOf(CourseAssignment a) => new
    {
        LessonsCompleted = a.LessonProgress.Count,
        LessonCount = a.Course!.Lessons.Count,
        AssessmentUnlocked = a.Course.Lessons.All(l => a.LessonProgress.Any(p => p.LessonId == l.Id)),
        a.Status
    };
}
