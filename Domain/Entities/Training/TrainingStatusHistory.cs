using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Training;

public class TrainingStatusHistory
{
    public int Id { get; set; }
    public int TrainingRequestId { get; set; }
    public TrainingRequest? TrainingRequest { get; set; }
    public TrainingAction Action { get; set; }
    public TrainingStatus? FromStatus { get; set; }
    public TrainingStatus ToStatus { get; set; }
    public int ActorId { get; set; }
    public Employee? Actor { get; set; }
    public string? Comment { get; set; }  // rejection/return/withdraw/reassign reason
    public DateTime CreatedAt { get; set; }
}