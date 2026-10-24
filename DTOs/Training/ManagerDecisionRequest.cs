namespace EmployeeSelfServiceAPI.DTOs.Training;

public class ManagerDecisionRequest
{
    public bool Approve { get; set; }
    public string? Comment { get; set; }
}