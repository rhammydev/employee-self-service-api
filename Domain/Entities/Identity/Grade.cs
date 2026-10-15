namespace EmployeeSelfServiceAPI.Domain.Entities.Identity;

public class Grade
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}