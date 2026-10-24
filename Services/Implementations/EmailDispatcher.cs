using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.Domain.Settings;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class EmailDispatcher(
    IEmailQueueRepository emailQueueRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IAppClock clock,
    IOptions<EmailQueueSettings> options,
    ILogger<EmailDispatcher> logger) : IEmailDispatcher
{
    private const int MaxErrorLength = 1000;
    private readonly EmailQueueSettings _settings = options.Value;

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        if (!emailSender.IsConfigured)
        {
            logger.LogWarning("SMTP is not configured; queued emails stay Pending.");
            return 0;
        }

        var now = clock.UtcNow;
        var claimedBy = $"{Environment.MachineName}:{Guid.NewGuid():N}";
        var ids = await emailQueueRepository.ClaimDueAsync(_settings.BatchSize, claimedBy, now,
            now.AddMinutes(-_settings.ClaimTimeoutMinutes), cancellationToken);

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = await emailQueueRepository.GetByIdAsync(id, cancellationToken);
            if (message == null || message.ClaimedBy != claimedBy) continue;

            await SendOneAsync(message, cancellationToken);

            await unitOfWork.SaveChangesAsync();
        }

        if (ids.Count > 0)
            logger.LogInformation("Email worker processed {Count} message(s).", ids.Count);

        return ids.Count;
    }

    private async Task SendOneAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        message.AttemptCount++;

        try
        {
            await emailSender.SendAsync(message.Recipient, message.Subject, message.Body, cancellationToken);

            message.Status = EmailStatus.Sent;
            message.SentAt = clock.UtcNow;
            message.LastErrorMessage = null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            message.AttemptCount--;
            message.Status = EmailStatus.Pending;
            message.ClaimedAt = null;
            message.ClaimedBy = null;
            await unitOfWork.SaveChangesAsync();
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Email {EmailId} failed on attempt {Attempt}.", message.Id, message.AttemptCount);
            message.LastErrorMessage = ex.Message.Length > MaxErrorLength ? ex.Message[..MaxErrorLength] : ex.Message;

            var retryDelays = _settings.RetryDelaysMinutes;
            if (message.AttemptCount <= retryDelays.Length)
            {
                message.Status = EmailStatus.Pending;
                message.NextAttemptAt = clock.UtcNow.AddMinutes(retryDelays[message.AttemptCount - 1]);
            }
            else
            {
                message.Status = EmailStatus.Failed;   
            }
        }

        message.ClaimedAt = null;
        message.ClaimedBy = null;
    }
}
