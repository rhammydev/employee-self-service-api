using EmployeeSelfServiceAPI.Data;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Training;
using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Repositories.Implementations;

public class TrainingRequestRepository(AppDbContext context): ITrainingRequestRepository
{
    public async Task AddAsync(TrainingRequest request) =>
        await context.TrainingRequests.AddAsync(request);

    public Task<TrainingRequest?> GetForUpdateAsync(int id) =>
        context.TrainingRequests
            .Include(t => t.Employee).ThenInclude(e => e!.ReportingManager)
            .Include(t => t.Employee).ThenInclude(e => e!.Grade)
            .Include(t => t.AssignedReviewer)
            .Include(t => t.Allowances)
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<TrainingRequest?> GetDetailsAsync(int id) =>
        context.TrainingRequests
            .AsNoTracking()
            .AsSplitQuery()   
            .Include(t => t.Employee)
            .Include(t => t.AssignedReviewer)
            .Include(t => t.StatusHistory).ThenInclude(h => h.Actor)
            .Include(t => t.Allowances).ThenInclude(a => a.CreatedBy)
            .Include(t => t.Allowances).ThenInclude(a => a.ApprovedBy)
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<PagedResult<TrainingRequest>> GetPagedForEmployeeAsync(int employeeId, TrainingRequestQuery query)
    {
        var requests = WithPeople().Where(t => t.EmployeeId == employeeId);

        if (query.Status.HasValue)
            requests = requests.Where(t => t.Status == query.Status);

        return requests.OrderByDescending(t => t.CreatedAt).ToPagedResultAsync(query);
    }

    public Task<PagedResult<TrainingRequest>> GetPagedPendingManagerAsync(int? reviewerId, PagedRequest request) =>
        WithPeople()
            .Where(t => t.Status == TrainingStatus.PendingManagerApproval
                        && (reviewerId == null || t.AssignedReviewerId == reviewerId))
            .OrderBy(t => t.SubmittedAt)
            .ToPagedResultAsync(request);

    public Task<PagedResult<TrainingRequest>> GetPagedEvidenceOverdueAsync(DateOnly endedBefore, PagedRequest request) =>
        WithPeople()
            .Where(t => t.Status == TrainingStatus.Approved && t.TrainingEndDate < endedBefore)
            .OrderBy(t => t.TrainingEndDate)
            .ToPagedResultAsync(request);

    public Task<PagedResult<TrainingRequest>> GetPagedByStatusesAsync(IReadOnlyCollection<TrainingStatus> statuses, PagedRequest request) =>
        WithPeople()
            .Where(t => statuses.Contains(t.Status))
            .OrderBy(t => t.UpdatedAt)
            .ToPagedResultAsync(request);

    private IQueryable<TrainingRequest> WithPeople() =>
        context.TrainingRequests.AsNoTracking()
            .Include(t => t.Employee)
            .Include(t => t.AssignedReviewer);
}