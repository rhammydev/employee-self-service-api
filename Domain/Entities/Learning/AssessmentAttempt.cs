namespace EmployeeSelfServiceAPI.Domain.Entities.Learning;

public class AssessmentAttempt
{
    public int Id { get; set; }
    public int CourseAssignmentId { get; set; }
    public CourseAssignment? CourseAssignment { get; set; }
    public int CycleNumber { get; set; }
    public int AttemptNumber { get; set; } // 1–3

    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Score { get; set; } // correct / total * 100
    public bool IsPassed { get; set; } // decided from the unrounded score
    public DateTime SubmittedAt { get; set; }

    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}