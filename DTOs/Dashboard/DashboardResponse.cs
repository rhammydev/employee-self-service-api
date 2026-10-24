using System.Text.Json.Serialization;

namespace EmployeeSelfServiceAPI.DTOs.Dashboard;

public class DashboardResponse
{
    public int PendingTraining { get; set; }
    public int IncompleteCourses { get; set; }
    public int OverdueCourses { get; set; }
    public int KpiPlansAwaitingAction { get; set; }
    public int ActiveApplications { get; set; }
    public int UnreadNotifications { get; set; }
    public int PublishedPayslipPeriods { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ManagerTrainingReviews { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? KpiReviews { get; set; }

    public DateTime GeneratedAt { get; set; }
}
