namespace EmployeeSelfServiceAPI.DTOs.Training;

public class SaveTrainingRequest
{
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
}