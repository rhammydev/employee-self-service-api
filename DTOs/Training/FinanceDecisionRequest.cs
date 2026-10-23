using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.DTOs.Training;

public class FinanceDecisionRequest
{
    public FinanceDecision Decision { get; set; }

    public int AllowanceVersion { get; set; }

    public string? Reason { get; set; }  
}
