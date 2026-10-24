using EmployeeSelfServiceAPI.DTOs.Performance;
using FluentValidation;

namespace EmployeeSelfServiceAPI.Validators;

public class CreateReviewPeriodRequestValidator : AbstractValidator<CreateReviewPeriodRequest>
{
    public CreateReviewPeriodRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate).WithMessage("Start date must not be after end date.");
        RuleFor(x => x.SubmissionDeadline).GreaterThanOrEqualTo(x => x.EndDate)
            .WithMessage("Submission deadline must be on or after the end date.");
    }
}

public class CreateKpiPlanRequestValidator : AbstractValidator<CreateKpiPlanRequest>
{
    public CreateKpiPlanRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.ReviewPeriodId).GreaterThan(0);
    }
}

public class SaveKpiItemRequestValidator : AbstractValidator<SaveKpiItemRequest>
{
    public SaveKpiItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Target).GreaterThan(0).WithMessage("Target must be greater than zero.");
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Weight).GreaterThan(0).WithMessage("Weight must be positive.")
            .LessThanOrEqualTo(100).PrecisionScale(5, 2, true).WithMessage("Weight can have at most 2 decimal places.");
    }
}

public class ActivateKpiPlanRequestValidator : AbstractValidator<ActivateKpiPlanRequest>
{
    public ActivateKpiPlanRequestValidator()
    {
        RuleFor(x => x.ReviewerId).GreaterThan(0).When(x => x.ReviewerId.HasValue);
    }
}

public class UpdateKpiProgressRequestValidator : AbstractValidator<UpdateKpiProgressRequest>
{
    public UpdateKpiProgressRequestValidator()
    {
        RuleFor(x => x.ActualValue).GreaterThanOrEqualTo(0).WithMessage("Actual value cannot be negative.");
        RuleFor(x => x.EvidenceComment).MaximumLength(2000);
    }
}

public class KpiReviewRequestValidator : AbstractValidator<KpiReviewRequest>
{
    public KpiReviewRequestValidator()
    {
        RuleFor(x => x.Comment).NotEmpty().When(x => !x.Approve).WithMessage("A comment is required when returning a plan.");
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}

public class ReopenKpiPlanRequestValidator : AbstractValidator<ReopenKpiPlanRequest>
{
    public ReopenKpiPlanRequestValidator()
    {
        RuleFor(x => x.NewDeadline).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
