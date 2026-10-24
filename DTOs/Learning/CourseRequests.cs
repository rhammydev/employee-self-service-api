using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Learning;

public class SaveCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassMark { get; set; } 
}

public class SaveLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? MaterialUrl { get; set; }
    public string? VideoUrl { get; set; }
}

public class SaveQuestionRequest
{
    public string Text { get; set; } = string.Empty;
    public List<SaveOptionRequest> Options { get; set; } = [];
}

public class SaveOptionRequest
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

// Full new order of lesson or question ids
public class ReorderRequest
{
    public List<int> Ids { get; set; } = [];
}

public class AssignCourseRequest
{
    public List<int> EmployeeIds { get; set; } = [];
    public DateOnly DueDate { get; set; }
}

public class CourseQuery : PagedRequest
{
    public CourseStatus? Status { get; set; }
    public string? Search { get; set; }
}

public class CourseAssignmentQuery : PagedRequest
{
    public AssignmentStatus? Status { get; set; }
    public bool? Overdue { get; set; }
}

public class SubmitAttemptRequest
{
    public List<AttemptAnswerRequest> Answers { get; set; } = [];

    public decimal? Score { get; set; }
}

public class AttemptAnswerRequest
{
    public int QuestionId { get; set; }
    public int SelectedOptionId { get; set; }
}
