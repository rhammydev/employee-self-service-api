namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IOrganizationRepository
{
    Task<bool> DepartmentExistsAsync(int id);
    Task<bool> GradeExistsAsync(int id);

    Task<List<int>> GetMissingDepartmentIdsAsync(IReadOnlyCollection<int> ids);
    Task<List<int>> GetMissingGradeIdsAsync(IReadOnlyCollection<int> ids);
}
