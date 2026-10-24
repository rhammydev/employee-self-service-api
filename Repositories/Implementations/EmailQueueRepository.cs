using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class EmailQueueRepository(AppDbContext context) : IEmailQueueRepository
{
    public async Task AddAsync(EmailMessage message) =>
        await context.EmailMessages.AddAsync(message);

    public Task<List<int>> ClaimDueAsync(int batchSize, string claimedBy, DateTime now, DateTime staleClaimBefore,
        CancellationToken cancellationToken) =>
        // One statement = one atomic claim. UPDLOCK + READPAST: a second worker skips rows
        // the first has locked instead of waiting for them or taking them too.
        // Also reclaims rows left in Processing by a run that crashed mid-send (at-least-once delivery).
        // Interpolated values become SQL parameters, not string concatenation.
        context.Database.SqlQuery<int>($"""
            WITH due AS (
                SELECT TOP ({batchSize}) *
                FROM EmailMessages WITH (ROWLOCK, UPDLOCK, READPAST)
                WHERE (Status = 'Pending' AND NextAttemptAt <= {now})
                   OR (Status = 'Processing' AND ClaimedAt < {staleClaimBefore})
                ORDER BY NextAttemptAt
            )
            UPDATE due
            SET Status = 'Processing', ClaimedAt = {now}, ClaimedBy = {claimedBy}
            OUTPUT inserted.Id AS Value;
            """).ToListAsync(cancellationToken);

    public Task<EmailMessage?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.EmailMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
}
