using EmployeeSelfServiceAPI.Domain.Entities.Identity;

namespace EmployeeSelfServiceAPI.Domain.Entities.Training;

public class EstacodeRate
{
    public int Id { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public int GradeId { get; set; }
    public Grade? Grade { get; set; }
    public decimal DailyRate { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}