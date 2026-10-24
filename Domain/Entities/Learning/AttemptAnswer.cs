namespace EmployeeSelfServiceAPI.Domain.Entities.Learning;

public class AttemptAnswer
{
    public int Id { get; set; }
    public int AssessmentAttemptId { get; set; }
    public AssessmentAttempt? AssessmentAttempt { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    public int SelectedOptionId { get; set; }
    public QuestionOption? SelectedOption { get; set; }
    public bool IsCorrect { get; set; }
}