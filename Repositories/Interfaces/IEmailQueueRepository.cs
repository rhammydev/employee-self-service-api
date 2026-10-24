using EmployeeSelfServiceAPI.Domain.Entities.Common;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IEmailQueueRepository
{
    Task AddAsync(EmailMessage message);

    Task<List<int>> ClaimDueAsync(int batchSize, string claimedBy, DateTime now, DateTime staleClaimBefore,
        CancellationToken cancellationToken);

    Task<EmailMessage?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task CancelPendingAsync(int employeeId, string subject, string reason);
}
