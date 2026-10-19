using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Data;

public class DbSeeder
{
    private const string DemoPassword = "Password@123";

    public static async Task SeedAsync(AppDbContext dbContext)
    {
        if (await dbContext.Employees.AnyAsync()) return;
        
        var now =  DateTime.UtcNow;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);
        
        var hr = new Department
        {
            Name = "Human Resources", 
            Description = "People and culture"
        };
        var finance = new Department
        {
            Name = "Finance", 
            Description = "Payroll and accounts"
        };
        var engineering = new Department
        {
            Name = "Engineering",
            Description = "Product engineering"
        };
        var operations = new Department
        {
            Name = "Operations",
            Description = "Business operations"
        };

        var gl08 = new Grade
        {
            Code = "GL-08", Name = "Officer", Level = 8
        };
        var gl10 = new Grade
        {
            Code = "GL-10", Name = "Senior Officer", Level = 10
        };
        var gl12 = new Grade
        {
            Code = "GL-12", Name = "Manager", Level = 12
        };
        var gl14 = new Grade
        {
            Code = "GL-14", Name = "Director", Level = 14
        };

        Employee NewEmployee(string number, string firstName, string lastName, Department department,
            Grade grade, Employee? manager, params UserRole[] extraRoles)
        {
            var employee = new Employee
            {
                EmployeeNumber = number,
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName}.{lastName}@ess.test".ToLowerInvariant(),
                PhoneNumber = $"0803{number[^4..]}000",
                PasswordHash = passwordHash,
                IsActive = true,
                Department = department,
                Grade = grade,
                ReportingManager = manager,
                CreatedAt = now,
                UpdatedAt = now
            };
            
            employee.Roles.Add(new EmployeeRole
            {
                Role = UserRole.Staff, AssignedAt = now
            });
            foreach (var role in extraRoles)
                employee.Roles.Add(new EmployeeRole { Role = role, AssignedAt = now });

            return employee;
        }
        
        var admin       = NewEmployee("ESS-0001", "Tunde",   "Bakare",  operations,  gl14, null,        UserRole.Admin, UserRole.Manager);
        var hrHead      = NewEmployee("ESS-0002", "Ngozi",   "Okafor",  hr,          gl14, admin,       UserRole.HR, UserRole.Manager);
        var hrOfficer   = NewEmployee("ESS-0003", "Bola",    "Adeyemi", hr,          gl10, hrHead,      UserRole.HR);
        var financeHead = NewEmployee("ESS-0004", "Chinedu", "Eze",     finance,     gl14, admin,       UserRole.Finance, UserRole.Manager);
        var financeOfficer = NewEmployee("ESS-0005", "Amina", "Bello",  finance,     gl10, financeHead, UserRole.Finance);
        var engManager  = NewEmployee("ESS-0006", "Ifeoma",  "Nwosu",   engineering, gl12, admin,       UserRole.Manager);
        var opsManager  = NewEmployee("ESS-0007", "Musa",    "Ibrahim", operations,  gl12, admin,       UserRole.Manager);
        var staff1      = NewEmployee("ESS-0008", "Kunle",   "Adebayo", engineering, gl08, engManager);
        var staff2      = NewEmployee("ESS-0009", "Zainab",  "Yusuf",   engineering, gl10, engManager);
        var staff3      = NewEmployee("ESS-0010", "Emeka",   "Obi",     operations,  gl08, opsManager);
        var inactive    = NewEmployee("ESS-0011", "Grace",   "Etim",    operations,  gl08, opsManager);
        inactive.IsActive = false;
        
        dbContext.Employees.AddRange(admin, hrHead, hrOfficer, financeHead, financeOfficer,
            engManager, opsManager, staff1, staff2, staff3, inactive);

        var baseRates = new Dictionary<string, decimal>
        {
            ["GB"] = 300m, ["US"] = 320m, ["AE"] = 250m, ["ZA"] = 180m, ["KE"] = 150m
        };
        
        foreach (var (country, baseRate) in baseRates)
        foreach (var grade in new[] { gl08, gl10, gl12, gl14 })
        {
            dbContext.EstacodeRates.Add(new EstacodeRate
            {
                CountryCode = country,
                Grade = grade,
                DailyRate = baseRate + (grade.Level - 8) * 20m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await dbContext.SaveChangesAsync();
    }
}