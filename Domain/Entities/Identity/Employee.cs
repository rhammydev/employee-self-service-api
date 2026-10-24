namespace EmployeeSelfServiceAPI.Domain.Entities.Identity;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; }  = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; }
    
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int GradeId { get; set; }
    public Grade? Grade { get; set; }
    
    public int? ReportingManagerId { get; set; }
    public Employee? ReportingManager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    public ICollection<EmployeeRole> Roles { get; set; } = new List<EmployeeRole>();

    public int FailedLoginCount { get; set; }
    public DateTime? FirstFailedLoginAt { get; set; }
    public DateTime? LockoutEndAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}