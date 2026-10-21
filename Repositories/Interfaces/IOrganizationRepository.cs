namespace EmployeeSelfServiceAPI.Repositories.Interfaces;

public interface IOrganizationRepository
{
    Task<bool> DepartmentExistsAsync(int id);
    Task<bool> GradeExistsAsync(int id);
}