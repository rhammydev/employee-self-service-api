namespace EmployeeSelfServiceAPI.Domain.Settings;

public class FileStorageSettings
{
    public string RootPath { get; set; } = "App_Data/uploads";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}