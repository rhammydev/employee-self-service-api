namespace EmployeeSelfServiceAPI.Domain.Entities.Learning;

public class Lesson
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? MaterialUrl { get; set; }
    public string? VideoUrl { get; set; }
    public int SortOrder { get; set; }
}