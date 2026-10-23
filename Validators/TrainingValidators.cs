using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Training;
using FluentValidation;

namespace EmployeeSelfServiceAPI.Validators;

public class SaveTrainingRequestValidator : AbstractValidator<SaveTrainingRequest>
{
    public SaveTrainingRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CountryCode).NotEmpty().Matches("^[A-Za-z]{2}$")
            .WithMessage("Country code must be a 2-letter ISO code, e.g. GB.");
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BusinessJustification).NotEmpty().MaximumLength(2000);

        RuleFor(x => x.TrainingStartDate).LessThanOrEqualTo(x => x.TrainingEndDate)
            .WithMessage("Training start must not be after training end.");
        RuleFor(x => x.DepartureDate).LessThanOrEqualTo(x => x.TrainingStartDate)
            .WithMessage("Departure must be on or before the training start date.");
        RuleFor(x => x.ReturnDate).GreaterThanOrEqualTo(x => x.TrainingEndDate)
            .WithMessage("Return must be on or after the training end date.");

        RuleFor(x => x.CourseFee).GreaterThanOrEqualTo(0).WithMessage("Course fee cannot be negative.");
        RuleFor(x => x.FlightEstimate).GreaterThanOrEqualTo(0).WithMessage("Flight estimate cannot be negative.");
        RuleFor(x => x.CourseFeeCurrency).NotEmpty().Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO code, e.g. USD.");
        RuleFor(x => x.FlightEstimateCurrency).NotEmpty().Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO code, e.g. USD.");
    }
}

public class ManagerDecisionRequestValidator : AbstractValidator<ManagerDecisionRequest>
{
    public ManagerDecisionRequestValidator()
    {
        RuleFor(x => x.Comment).NotEmpty().When(x => !x.Approve)
            .WithMessage("A comment is required when rejecting.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public class ReassignReviewerRequestValidator : AbstractValidator<ReassignReviewerRequest>
{
    public ReassignReviewerRequestValidator()
    {
        RuleFor(x => x.NewReviewerId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class ReasonRequestValidator : AbstractValidator<ReasonRequest>
{
    public ReasonRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required.").MaximumLength(1000);
    }
}

public class FinanceDecisionRequestValidator : AbstractValidator<FinanceDecisionRequest>
{
    public FinanceDecisionRequestValidator()
    {
        RuleFor(x => x.Decision).IsInEnum().WithMessage("Decision must be Approve, Reject or ReturnToHR.");
        RuleFor(x => x.AllowanceVersion).GreaterThan(0).WithMessage("The allowance version you reviewed is required.");
        RuleFor(x => x.Reason).NotEmpty().When(x => x.Decision != FinanceDecision.Approve)
            .WithMessage("A reason is required when rejecting or returning to HR.");
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public class AllowanceCorrectionRequestValidator : AbstractValidator<AllowanceCorrectionRequest>
{
    public AllowanceCorrectionRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.DailyRate.HasValue || x.EligibleDays.HasValue)
            .WithName("correction")
            .WithMessage("Provide a new daily rate, new eligible days, or both.");
        RuleFor(x => x.DailyRate).GreaterThan(0).When(x => x.DailyRate.HasValue)
            .WithMessage("Daily rate must be greater than zero.");
        RuleFor(x => x.EligibleDays).GreaterThanOrEqualTo(0).When(x => x.EligibleDays.HasValue)
            .WithMessage("Eligible days cannot be negative.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required for a correction.").MaximumLength(1000);
    }
}