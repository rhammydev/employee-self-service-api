using EmployeeSelfServiceAPI.Domain.Settings;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmployeeSelfServiceAPI.Workers;

public class EmailQueueWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailQueueSettings> options,
    ILogger<EmailQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds));

        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Email worker stopping.");
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IEmailDispatcher>();
            await dispatcher.ProcessDueAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email worker run failed.");
        }
    }
}
