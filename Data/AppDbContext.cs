using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Entities.Opportunities;
using EmployeeSelfServiceAPI.Domain.Entities.Payroll;
using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Identity
    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeeRole> EmployeeRoles { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<AccountToken> AccountTokens { get; set; }

    // Common
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<EmailMessage> EmailMessages { get; set; }
    public DbSet<ReminderLog> ReminderLogs { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    // Training
    public DbSet<TrainingRequest> TrainingRequests { get; set; }
    public DbSet<TrainingStatusHistory> TrainingStatusHistories { get; set; }
    public DbSet<EstacodeRate> EstacodeRates { get; set; }
    public DbSet<EstacodeAllowance> EstacodeAllowances { get; set; }

    // Learning
    public DbSet<Course> Courses { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<CourseAssignment> CourseAssignments { get; set; }
    public DbSet<LessonProgress> LessonProgress { get; set; }
    public DbSet<AssessmentAttempt> AssessmentAttempts { get; set; }
    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }
    public DbSet<AssignmentReset> AssignmentResets { get; set; }

    // Performance
    public DbSet<ReviewPeriod> ReviewPeriods { get; set; }
    public DbSet<KpiPlan> KpiPlans { get; set; }
    public DbSet<KpiItem> KpiItems { get; set; }
    public DbSet<KpiDeadlineReopening> KpiDeadlineReopenings { get; set; }
    public DbSet<KpiPlanStatusHistory> KpiPlanStatusHistories { get; set; }

    // Payroll
    public DbSet<Payslip> Payslips { get; set; }
    public DbSet<PayslipVersion> PayslipVersions { get; set; }
    public DbSet<PayslipLineItem> PayslipLineItems { get; set; }

    // Opportunities
    public DbSet<Opportunity> Opportunities { get; set; }
    public DbSet<OpportunityAllowedDepartment> OpportunityAllowedDepartments { get; set; }
    public DbSet<OpportunityAllowedGrade> OpportunityAllowedGrades { get; set; }
    public DbSet<OpportunityApplication> OpportunityApplications { get; set; }
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentity(modelBuilder);
        ConfigureCommon(modelBuilder);
        ConfigureTraining(modelBuilder);
        ConfigureLearning(modelBuilder);
        ConfigurePerformance(modelBuilder);
        ConfigurePayroll(modelBuilder);
        ConfigureOpportunities(modelBuilder);

        ApplyConventions(modelBuilder);
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.EmployeeNumber).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.EmployeeNumber).HasMaxLength(20);
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);

            e.HasOne(x => x.ReportingManager)
                .WithMany(x => x.DirectReports)
                .HasForeignKey(x => x.ReportingManagerId);
        });

        modelBuilder.Entity<EmployeeRole>()
            .HasIndex(x => new { x.EmployeeId, x.Role })
            .IsUnique();

        modelBuilder.Entity<Department>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Grade>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(20);
            e.Property(x => x.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<AccountToken>(e =>
        {
            e.HasIndex(x => x.TokenHash);
            e.HasIndex(x => new { x.EmployeeId, x.Purpose });
            e.Property(x => x.TokenHash).HasMaxLength(128);
        });
    }

    private static void ConfigureCommon(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>()
            .HasIndex(x => new { x.EmployeeId, x.IsRead });

        modelBuilder.Entity<EmailMessage>()
            .HasIndex(x => new { x.Status, x.NextAttemptAt });

        modelBuilder.Entity<ReminderLog>(e =>
        {
            e.HasIndex(x => new { x.RecipientId, x.ItemType, x.ItemId, x.ReminderType, x.ReminderDate })
                .IsUnique();
            e.Property(x => x.ItemType).HasMaxLength(50);
            e.Property(x => x.ReminderType).HasMaxLength(50);
        });

        modelBuilder.Entity<Attachment>(e =>
        {
            e.HasIndex(x => new { x.ParentType, x.ParentId });
            e.Property(x => x.OriginalFileName).HasMaxLength(255);
            e.Property(x => x.StoredFileName).HasMaxLength(255);
            e.Property(x => x.ContentType).HasMaxLength(100);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.TargetEntity).HasMaxLength(50);
            e.Property(x => x.Details).HasMaxLength(500);
            e.HasIndex(x => new { x.TargetEntity, x.TargetId });
        });
    }

    private static void ConfigureTraining(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrainingRequest>(e =>
        {
            e.HasIndex(x => x.Reference).IsUnique();
            e.HasIndex(x => new { x.Status, x.AssignedReviewerId });
            e.Property(x => x.Reference).HasMaxLength(30);
            e.Property(x => x.CountryCode).HasMaxLength(2);
            e.Property(x => x.CourseFeeCurrency).HasMaxLength(3);
            e.Property(x => x.FlightEstimateCurrency).HasMaxLength(3);
        });

        modelBuilder.Entity<EstacodeRate>(e =>
        {
            e.HasIndex(x => new { x.CountryCode, x.GradeId }).IsUnique();
            e.Property(x => x.CountryCode).HasMaxLength(2);
            e.Property(x => x.Currency).HasMaxLength(3);
        });

        modelBuilder.Entity<EstacodeAllowance>(e =>
        {
            e.HasIndex(x => new { x.TrainingRequestId, x.VersionNumber }).IsUnique();
            e.Property(x => x.CountryCode).HasMaxLength(2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.GradeCode).HasMaxLength(20);
        });
    }

    private static void ConfigureLearning(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseAssignment>()
            .HasIndex(x => new { x.CourseId, x.EmployeeId })
            .IsUnique();

        modelBuilder.Entity<LessonProgress>(e =>
        {
            e.HasIndex(x => new { x.CourseAssignmentId, x.LessonId }).IsUnique();
            e.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssessmentAttempt>(e =>
        {
            e.HasIndex(x => new { x.CourseAssignmentId, x.CycleNumber, x.AttemptNumber }).IsUnique();
            e.Property(x => x.Score).HasPrecision(7, 4);
        });

        modelBuilder.Entity<AttemptAnswer>(e =>
        {
            e.HasIndex(x => new { x.AssessmentAttemptId, x.QuestionId }).IsUnique();
            e.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SelectedOption)
                .WithMany()
                .HasForeignKey(x => x.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePerformance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KpiPlan>(e =>
        {
            e.HasIndex(x => new { x.EmployeeId, x.ReviewPeriodId }).IsUnique();
            e.Property(x => x.OverallScore).HasPrecision(9, 4);
        });

        modelBuilder.Entity<KpiItem>(e =>
        {
            e.Property(x => x.Target).HasPrecision(18, 4);
            e.Property(x => x.ActualValue).HasPrecision(18, 4);
            e.Property(x => x.Weight).HasPrecision(5, 2);
            e.Property(x => x.AchievementPercent).HasPrecision(9, 4);
            e.Property(x => x.WeightedScore).HasPrecision(9, 4);
            e.Property(x => x.Unit).HasMaxLength(50);
        });
    }

    private static void ConfigurePayroll(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payslip>()
            .HasIndex(x => new { x.EmployeeId, x.Year, x.Month })
            .IsUnique();

        modelBuilder.Entity<PayslipVersion>(e =>
        {
            e.HasIndex(x => new { x.PayslipId, x.VersionNumber }).IsUnique();

            e.HasIndex(x => x.PayslipId, "IX_PayslipVersions_PayslipId_Draft")
                .IsUnique()
                .HasFilter("[Status] = 'Draft'");

            e.HasIndex(x => x.PayslipId, "IX_PayslipVersions_PayslipId_Published")
                .IsUnique()
                .HasFilter("[Status] = 'Published'");

            e.Property(x => x.Currency).HasMaxLength(3);
        });

        modelBuilder.Entity<PayslipLineItem>()
            .Property(x => x.Name)
            .HasMaxLength(100);
    }

    private static void ConfigureOpportunities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Opportunity>()
            .HasIndex(x => new { x.Status, x.ClosingAt });

        modelBuilder.Entity<OpportunityAllowedDepartment>()
            .HasKey(x => new { x.OpportunityId, x.DepartmentId });

        modelBuilder.Entity<OpportunityAllowedGrade>()
            .HasKey(x => new { x.OpportunityId, x.GradeId });

        modelBuilder.Entity<OpportunityApplication>()
            .HasIndex(x => new { x.OpportunityId, x.EmployeeId })
            .IsUnique();
    }

    private static void ApplyConventions(ModelBuilder modelBuilder)
    {
        var restrictPrincipals = new[] { typeof(Employee), typeof(Department), typeof(Grade) };

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                if (type.IsEnum)
                {
                    property.SetProviderClrType(typeof(string));
                    property.SetMaxLength(50);
                }

                if (type == typeof(decimal) && property.GetPrecision() == null)
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                if (restrictPrincipals.Contains(foreignKey.PrincipalEntityType.ClrType))
                    foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
