using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.DTOs.Training;

public class TrainingRequestSummaryResponse
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateOnly TrainingStartDate { get; set; }
    public DateOnly TrainingEndDate { get; set; }
    public TrainingStatus Status { get; set; }
    public string? AssignedReviewerName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}