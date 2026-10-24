namespace EmployeeSelfServiceAPI.Domain.Entities.Performance;

public class KpiItem
{
    public int Id { get; set; }
    public int KpiPlanId { get; set; }
    public KpiPlan? KpiPlan { get; set; }

    // HR-owned (editable only while plan is Draft)
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Target { get; set; } 
    public string Unit { get; set; } = string.Empty;
    public decimal Weight { get; set; }                         
    public int SortOrder { get; set; }

    // Staff-owned (editable in Active/Returned before the deadline)
    public decimal? ActualValue { get; set; }  
    public string? EvidenceComment { get; set; }
    
    public decimal? AchievementPercent { get; set; } // min(actual / target * 100)
    public decimal? WeightedScore { get; set; } // achievement / 100 * weight
}