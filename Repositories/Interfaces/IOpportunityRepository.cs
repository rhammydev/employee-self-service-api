using EmployeeSelfServiceAPI.Domain.Entities.Opportunities;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Opportunities;

namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IOpportunityRepository
{
    Task AddAsync(Opportunity opportunity);

    Task<Opportunity?> GetForUpdateAsync(int id);

    Task<Opportunity?> GetDetailsAsync(int id);

    Task<PagedResult<Opportunity>> GetPagedAsync(OpportunityQuery query, bool publishedOnly, DateTime now);

    void RemoveAllowedDepartment(OpportunityAllowedDepartment item);
    void RemoveAllowedGrade(OpportunityAllowedGrade item);

    // Applications
    Task AddApplicationAsync(OpportunityApplication application);
    Task<OpportunityApplication?> GetApplicationIdForEmployeeAsync(int opportunityId, int employeeId);

    Task<OpportunityApplication?> GetApplicationForUpdateAsync(int id);

    Task<OpportunityApplication?> GetApplicationDetailsAsync(int id);

    Task<PagedResult<OpportunityApplication>> GetPagedApplicationsForOpportunityAsync(int opportunityId, ApplicationQuery query);
    Task<PagedResult<OpportunityApplication>> GetPagedApplicationsForEmployeeAsync(int employeeId, ApplicationQuery query);
}
