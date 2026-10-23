using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class EstacodeRateRepository(AppDbContext context) : IEstacodeRateRepository
{
    public Task<EstacodeRate?> GetActiveRateAsync(string countryCode, int gradeId) =>
        context.EstacodeRates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.CountryCode == countryCode && r.GradeId == gradeId && r.IsActive);
}
