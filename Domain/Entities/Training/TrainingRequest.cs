using System.ComponentModel.DataAnnotations;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Enums;

namespace EmployeeSelfServiceAPI.Domain.Entities.Training;

public class TrainingRequest
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

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
    public Employee? AssignedReviewer { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<EstacodeAllowance> Allowances { get; set; } = new List<EstacodeAllowance>();
    public ICollection<TrainingStatusHistory> StatusHistory { get; set; } = new List<TrainingStatusHistory>();
}