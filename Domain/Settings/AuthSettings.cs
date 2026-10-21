namespace EmployeeSelfServiceAPI.Domain.Settings;

public class AuthSettings
{
    public int SetupTokenExpiryHours { get; set; } = 48;      
    public int ResetTokenExpiryMinutes { get; set; } = 30;
    public int MaxFailedLogins { get; set; } = 5;          
    public int FailedLoginWindowMinutes { get; set; } = 15;
    public int LockoutMinutes { get; set; } = 15;
    public string ClientBaseUrl { get; set; } = string.Empty;
}