using EmployeeSelfServiceAPI.Domain.Settings;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Services.Implementations;
using EmployeeSelfServiceAPI.Services.Interfaces;
using EmployeeSelfServiceAPI.Workers;

namespace EmployeeSelfServiceAPI.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthSettings>(configuration.GetSection("Auth"));

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        
        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));

        services.AddSingleton<IAppClock, AppClock>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ITrainingRequestService, TrainingRequestService>();
        services.AddScoped<ITrainingApprovalService, TrainingApprovalService>();
        services.AddSingleton<IPdfService, PdfService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ICourseAssignmentService, CourseAssignmentService>();
        services.AddScoped<IKpiPlanService, KpiPlanService>();
        services.AddScoped<IKpiProgressService, KpiProgressService>();
        services.AddScoped<IPayslipService, PayslipService>();
        services.AddScoped<IOpportunityService, OpportunityService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Daily reminders (NOT-02)
        services.Configure<ReminderSettings>(configuration.GetSection("Reminders"));
        services.AddScoped<IReminderService, ReminderService>();
        services.AddHostedService<ReminderWorker>();

        // Email queue: business code only queues; the worker sends
        services.Configure<SmtpMail>(configuration.GetSection("SmtpMail"));
        services.Configure<EmailQueueSettings>(configuration.GetSection("EmailQueue"));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailDispatcher, EmailDispatcher>();
        services.AddHostedService<EmailQueueWorker>();

        return services;
    }
}