using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class AccountTokenRepository(AppDbContext context) : IAccountTokenRepository
{
    public async Task AddAsync(AccountToken token) =>
        await context.AccountTokens.AddAsync(token);

    public Task<AccountToken?> GetUsableAsync(string tokenHash, TokenPurpose purpose)
    {
        return context.AccountTokens.Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash 
                                      && t.Purpose == purpose
                                      && t.UsedAt == null
                                      && t.InvalidatedAt == null
                                      && t.ExpiresAt > DateTime.UtcNow);
    }

    public async Task InvalidateActiveAsync(int employeeId, TokenPurpose purpose)
    {
        var now = DateTime.UtcNow;
        var tokens = await context.AccountTokens
            .Where(t => t.EmployeeId == employeeId && t.Purpose == purpose
                                                   && t.UsedAt == null && t.InvalidatedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
            token.InvalidatedAt = now;
    }
}