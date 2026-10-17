using System.ComponentModel.DataAnnotations;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Learning;

public class CourseAssignment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int AssignedById { get; set; }
    public Employee? AssignedBy { get; set; }

    public DateOnly DueDate { get; set; }
    public AssignmentStatus Status { get; set; }
    public int CurrentCycle { get; set; } = 1;
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
    public ICollection<AssessmentAttempt> Attempts { get; set; } = new List<AssessmentAttempt>();
    public ICollection<AssignmentReset> Resets { get; set; } = new List<AssignmentReset>();
}