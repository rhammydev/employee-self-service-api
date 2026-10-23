using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.DTOs.Training;

public class TrainingHistoryResponse
{
    public TrainingAction Action { get; set; }
    public TrainingStatus? FromStatus { get; set; }
    public TrainingStatus ToStatus { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}