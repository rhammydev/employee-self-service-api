namespace EmployeeSelfServiceAPI.Domain.Entities.Identity;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}