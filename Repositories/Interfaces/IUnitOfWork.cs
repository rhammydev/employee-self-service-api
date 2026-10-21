namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}