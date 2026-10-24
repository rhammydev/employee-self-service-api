using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Training;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface ITrainingRequestRepository
{
    Task AddAsync(TrainingRequest request);

    Task<TrainingRequest?> GetForUpdateAsync(int id);

    Task<TrainingRequest?> GetDetailsAsync(int id);

    Task<PagedResult<TrainingRequest>> GetPagedForEmployeeAsync(int employeeId, TrainingRequestQuery query);

    Task<PagedResult<TrainingRequest>> GetPagedPendingManagerAsync(int? reviewerId, PagedRequest request);

    Task<PagedResult<TrainingRequest>> GetPagedByStatusesAsync(IReadOnlyCollection<TrainingStatus> statuses, PagedRequest request);

    Task<PagedResult<TrainingRequest>> GetPagedEvidenceOverdueAsync(DateOnly endedBefore, PagedRequest request);
}