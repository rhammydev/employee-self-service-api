using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync() => context.SaveChangesAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync() => context.Database.BeginTransactionAsync();
}
