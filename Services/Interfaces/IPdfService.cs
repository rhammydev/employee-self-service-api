using EmployeeSelfServiceAPI.Domain.Entities.Training;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IPdfService
{
    byte[] GenerateAllowanceStatement(TrainingRequest training, EstacodeAllowance allowance, DateTime generatedAtUtc);
}
