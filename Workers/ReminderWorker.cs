using EmployeeSelfServiceAPI.Domain.Settings;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmployeeSelfServiceAPI.Workers;

public class ReminderWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReminderSettings> options,
    ILogger<ReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.IntervalMinutes));

        try
        {
            do
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IReminderService>().RunAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Reminder run failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Reminder worker stopping.");
        }
    }
}
