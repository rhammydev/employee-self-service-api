namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IEmailDispatcher
{
    Task<int> ProcessDueAsync(CancellationToken cancellationToken);
}
