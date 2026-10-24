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
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseAssignmentRepository, CourseAssignmentRepository>();
        services.AddScoped<IKpiRepository, KpiRepository>();
        services.AddScoped<IPayslipRepository, PayslipRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        return services;
    }
}