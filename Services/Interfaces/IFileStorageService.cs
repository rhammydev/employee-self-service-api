using EmployeeSelfServiceAPI.Helpers;

namespace EmployeeSelfServiceAPI.Services.Interfaces;

public interface IFileStorageService
{
    Task<(AllowedFileType? Type, string? Error)> ValidateAsync(IFormFile file, IReadOnlyCollection<AllowedFileType> allowed);
    Task<string> SaveAsync(IFormFile file, AllowedFileType type); 
    Stream? OpenRead(string storedFileName);
}