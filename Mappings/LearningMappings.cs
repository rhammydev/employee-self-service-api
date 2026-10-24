using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Learning;
using EmployeeSelfServiceAPI.Helpers;

namespace EmployeeSelfServiceAPI.Mappings;

public static class LearningMappings
{
    public static CourseSummaryResponse ToCourseSummary(Course c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Status = c.Status,
        PassMark = c.PassMark,
        LessonCount = c.Lessons.Count,
        QuestionCount = c.Questions.Count,
        PublishedAt = c.PublishedAt,
        CreatedAt = c.CreatedAt
    };

    public static CourseDetailResponse ToCourseDetail(Course c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Description = c.Description,
        PassMark = c.PassMark,
        Status = c.Status,
        CreatedByName = c.CreatedBy == null ? string.Empty : $"{c.CreatedBy.FirstName} {c.CreatedBy.LastName}",
        CreatedAt = c.CreatedAt,
        PublishedAt = c.PublishedAt,
        Lessons = c.Lessons.OrderBy(l => l.SortOrder).Select(l => ToLesson(l, null)).ToList(),
        Questions = c.Questions.OrderBy(q => q.SortOrder).Select(q => ToQuestion(q, includeAnswerKey: true)).ToList()
    };

    public static AssignmentSummaryResponse ToAssignmentSummary(CourseAssignment a, DateOnly today)
    {
        var lessonIds = a.Course?.Lessons.Select(l => l.Id).ToHashSet() ?? [];
        var attemptsUsed = a.Attempts.Count(x => x.CycleNumber == a.CurrentCycle);
        var canAttempt = a.Status is AssignmentStatus.NotStarted or AssignmentStatus.InProgress;

        return new AssignmentSummaryResponse
        {
            Id = a.Id,
            CourseId = a.CourseId,
            CourseTitle = a.Course?.Title ?? string.Empty,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee == null ? string.Empty : $"{a.Employee.FirstName} {a.Employee.LastName}",
            Status = a.Status,
            DueDate = a.DueDate,
            IsOverdue = a.DueDate < today && a.Status != AssignmentStatus.Completed,
            LessonsCompleted = a.LessonProgress.Count(p => lessonIds.Contains(p.LessonId)),
            LessonCount = lessonIds.Count,
            CurrentCycle = a.CurrentCycle,
            AttemptsUsed = attemptsUsed,
            AttemptsRemaining = canAttempt ? Math.Max(0, AssessmentScorer.MaxAttemptsPerCycle - attemptsUsed) : 0,
            BestScore = a.Attempts.Count == 0 ? null : Round2(a.Attempts.Max(x => x.Score)),
            CompletedAt = a.CompletedAt
        };
    }

    public static AssignmentDetailResponse ToAssignmentDetail(CourseAssignment a, DateOnly today)
    {
        var course = a.Course!;
        var completedLessonIds = a.LessonProgress.Select(p => p.LessonId).ToHashSet();
        var summary = ToAssignmentSummary(a, today);

        return new AssignmentDetailResponse
        {
            Summary = summary,
            CourseDescription = course.Description,
            PassMark = course.PassMark,
            AssessmentUnlocked = summary.LessonCount > 0 && summary.LessonsCompleted == summary.LessonCount,
            Lessons = course.Lessons.OrderBy(l => l.SortOrder)
                .Select(l => ToLesson(l, completedLessonIds.Contains(l.Id))).ToList(),
            Questions = course.Questions.OrderBy(q => q.SortOrder)
                .Select(q => ToQuestion(q, includeAnswerKey: false)).ToList(),
            Attempts = a.Attempts.OrderBy(x => x.CycleNumber).ThenBy(x => x.AttemptNumber).Select(ToAttempt).ToList(),
            Resets = a.Resets.OrderBy(r => r.ResetAt).Select(r => new ResetResponse
            {
                FromCycle = r.FromCycle,
                ToCycle = r.ToCycle,
                Reason = r.Reason,
                ResetByName = r.ResetBy == null ? string.Empty : $"{r.ResetBy.FirstName} {r.ResetBy.LastName}",
                ResetAt = r.ResetAt
            }).ToList()
        };
    }

    public static AttemptResponse ToAttempt(AssessmentAttempt x) => new()
    {
        CycleNumber = x.CycleNumber,
        AttemptNumber = x.AttemptNumber,
        CorrectCount = x.CorrectCount,
        TotalQuestions = x.TotalQuestions,
        Score = Round2(x.Score),
        IsPassed = x.IsPassed,
        SubmittedAt = x.SubmittedAt
    };

    private static LessonResponse ToLesson(Lesson l, bool? isCompleted) => new()
    {
        Id = l.Id,
        Title = l.Title,
        Content = l.Content,
        MaterialUrl = l.MaterialUrl,
        VideoUrl = l.VideoUrl,
        SortOrder = l.SortOrder,
        IsCompleted = isCompleted
    };

    private static QuestionResponse ToQuestion(Question q, bool includeAnswerKey) => new()
    {
        Id = q.Id,
        Text = q.Text,
        SortOrder = q.SortOrder,
        Options = q.Options.OrderBy(o => o.SortOrder).Select(o => new OptionResponse
        {
            Id = o.Id,
            Text = o.Text,
            SortOrder = o.SortOrder,
            IsCorrect = includeAnswerKey ? o.IsCorrect : null
        }).ToList()
    };

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
