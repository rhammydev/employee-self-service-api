using EmployeeSelfServiceAPI.Repositories.Implementations;
using EmployeeSelfServiceAPI.Repositories.Interfaces;

namespace EmployeeSelfServiceAPI.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        return services;
    }
}