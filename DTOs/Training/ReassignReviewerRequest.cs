namespace EmployeeSelfServiceAPI.DTOs.Training;

public class ReassignReviewerRequest
{
    public int NewReviewerId { get; set; }
    public string Reason { get; set; } = string.Empty;
}