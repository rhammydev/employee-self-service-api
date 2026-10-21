using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IAccountTokenRepository
{
    Task AddAsync(AccountToken token);
    Task<AccountToken?> GetUsableAsync(string tokenHash, TokenPurpose purpose);
    Task InvalidateActiveAsync(int employeeId, TokenPurpose purpose);
}