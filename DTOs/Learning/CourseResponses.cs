using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.DTOs.Learning;

public class CourseSummaryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public CourseStatus Status { get; set; }
    public int PassMark { get; set; }
    public int LessonCount { get; set; }
    public int QuestionCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CourseDetailResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassMark { get; set; }
    public CourseStatus Status { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<LessonResponse> Lessons { get; set; } = [];
    public List<QuestionResponse> Questions { get; set; } = [];
}

public class LessonResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? MaterialUrl { get; set; }
    public string? VideoUrl { get; set; }
    public int SortOrder { get; set; }
    public bool? IsCompleted { get; set; }  
}

public class QuestionResponse
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<OptionResponse> Options { get; set; } = [];
}

public class OptionResponse
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsCorrect { get; set; }
}

public class AssignmentSummaryResponse
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public AssignmentStatus Status { get; set; }
    public DateOnly DueDate { get; set; }
    public bool IsOverdue { get; set; }                 
    public int LessonsCompleted { get; set; }
    public int LessonCount { get; set; }
    public int CurrentCycle { get; set; }
    public int AttemptsUsed { get; set; }                
    public int AttemptsRemaining { get; set; }
    public decimal? BestScore { get; set; }             
    public DateTime? CompletedAt { get; set; }
}

public class AssignmentDetailResponse
{
    public AssignmentSummaryResponse Summary { get; set; } = new();
    public string? CourseDescription { get; set; }
    public int PassMark { get; set; }
    public bool AssessmentUnlocked { get; set; }
    public List<LessonResponse> Lessons { get; set; } = [];
    public List<QuestionResponse> Questions { get; set; } = [];
    public List<AttemptResponse> Attempts { get; set; } = [];
    public List<ResetResponse> Resets { get; set; } = [];
}

public class AttemptResponse
{
    public int CycleNumber { get; set; }
    public int AttemptNumber { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Score { get; set; }                   
    public bool IsPassed { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class AttemptResultResponse : AttemptResponse
{
    public AssignmentStatus AssignmentStatus { get; set; }
    public int AttemptsRemaining { get; set; }
    public bool ClientScoreIgnored { get; set; }
}

public class ResetResponse
{
    public int FromCycle { get; set; }
    public int ToCycle { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ResetByName { get; set; } = string.Empty;
    public DateTime ResetAt { get; set; }
}

public class AssignResultResponse
{
    public List<AssignResultItem> Results { get; set; } = [];
    public int AssignedCount => Results.Count(r => r.Assigned);
}

public class AssignResultItem
{
    public int EmployeeId { get; set; }
    public bool Assigned { get; set; }
    public int? AssignmentId { get; set; }
    public int StatusCode { get; set; }                 
    public string Message { get; set; } = string.Empty;
}
