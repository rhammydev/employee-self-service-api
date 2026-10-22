using EmployeeSelfServiceAPI.Domain.Settings;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class FileStorageService(IOptions<FileStorageSettings> options, IWebHostEnvironment environment) : IFileStorageService
{
    private readonly FileStorageSettings _settings = options.Value;
    private readonly string _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));
    
    public async Task<(AllowedFileType? Type, string? Error)> ValidateAsync(IFormFile file, IReadOnlyCollection<AllowedFileType> allowed)
    {
        var allowedNames = string.Join(", ", allowed.Select(a => a.Extensions[0].TrimStart('.').ToUpperInvariant()));
        
        if(file.Length == 0)
            return (null, "The file is empty.");

        if (file.Length > _settings.MaxFileSizeBytes)
            return (null, $"The file must not exceed {_settings.MaxFileSizeBytes / 1024 / 1024} MB.");
        
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var type = allowed.FirstOrDefault(a => a.Extensions.Contains(extension));
        if (type == null)
            return (null, $"Only {allowedNames} files are supported.");
        
        var header = new byte[type.Signature.Length];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        if(read < header.Length || !header.AsSpan().SequenceEqual(type.Signature))
            return (null, $"The file content is not a valid {type.Extensions[0].TrimStart('.').ToUpperInvariant()} file.");

        return (type, null);
    }

    public async Task<string> SaveAsync(IFormFile file, AllowedFileType type)
    {
        Directory.CreateDirectory(_root);

        var storedFileName = $"{Guid.NewGuid():N}{type.Extensions[0]}";
        await using var target = File.Create(Path.Combine(_root, storedFileName));
        await file.CopyToAsync(target);
        
        return storedFileName;
    }

    public Stream? OpenRead(string storedFileName)
    {
        var path = Path.GetFullPath(Path.Combine(_root, storedFileName));
        if(!path.StartsWith(_root, StringComparison.Ordinal) || !File.Exists(path))
            return null;
        
        return File.OpenRead(path);
    }
}