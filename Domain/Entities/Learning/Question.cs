namespace EmployeeSelfServiceAPI.Domain.Entities.Learning;

public class Question
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}