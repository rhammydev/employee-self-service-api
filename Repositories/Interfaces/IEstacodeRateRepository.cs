using EmployeeSelfServiceAPI.Domain.Entities.Training;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IEstacodeRateRepository
{
    Task<EstacodeRate?> GetActiveRateAsync(string countryCode, int gradeId);
}
