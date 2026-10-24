using Microsoft.EntityFrameworkCore.Storage;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();

    Task<IDbContextTransaction> BeginTransactionAsync();
}
