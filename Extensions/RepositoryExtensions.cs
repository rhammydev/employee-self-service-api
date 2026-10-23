using EmployeeSelfServiceAPI.Repositories.Implementations;
using EmployeeSelfServiceAPI.Repositories.Interfaces;

namespace EmployeeSelfServiceAPI.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAccountTokenRepository, AccountTokenRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<ITrainingRequestRepository, TrainingRequestRepository>();
        services.AddScoped<IEstacodeRateRepository, EstacodeRateRepository>();

        return services;
    }
}