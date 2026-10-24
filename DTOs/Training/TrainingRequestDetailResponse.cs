using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;

namespace EmployeeSelfServiceAPI.DTOs.Training;

public class TrainingRequestDetailResponse
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateOnly TrainingStartDate { get; set; }
    public DateOnly TrainingEndDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public string BusinessJustification { get; set; } = string.Empty;
    public decimal CourseFee { get; set; }
    public string CourseFeeCurrency { get; set; } = string.Empty;
    public decimal FlightEstimate { get; set; }
    public string FlightEstimateCurrency { get; set; } = string.Empty;
    public TrainingStatus Status { get; set; }
    public int? AssignedReviewerId { get; set; }
    public string? AssignedReviewerName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AttachmentResponse> Attachments { get; set; } = [];
    public List<TrainingHistoryResponse> History { get; set; } = [];
    public List<AllowanceResponse> Allowances { get; set; } = [];
}