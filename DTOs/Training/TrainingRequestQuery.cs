using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Training;

public class TrainingRequestQuery : PagedRequest
{
    public TrainingStatus? Status { get; set; }
}