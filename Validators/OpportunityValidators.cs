using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Opportunities;
using FluentValidation;

namespace EmployeeSelfServiceAPI.Validators;

public class SaveOpportunityRequestValidator : AbstractValidator<SaveOpportunityRequest>
{
    public SaveOpportunityRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Eligibility).MaximumLength(2000);
        RuleFor(x => x.ClosingAt).NotEmpty();
        RuleFor(x => x.AllowedDepartmentIds).Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Department ids must not repeat.");
        RuleFor(x => x.AllowedGradeIds).Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Grade ids must not repeat.");
    }
}

public class ApplyRequestValidator : AbstractValidator<ApplyRequest>
{
    public ApplyRequestValidator()
    {
        RuleFor(x => x.SupportingStatement).NotEmpty().WithMessage("A supporting statement is required.").MaximumLength(4000);
    }
}

public class UpdateApplicationStatusRequestValidator : AbstractValidator<UpdateApplicationStatusRequest>
{
    public UpdateApplicationStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is ApplicationStatus.Shortlisted or ApplicationStatus.Selected or ApplicationStatus.Rejected)
            .WithMessage("Status must be Shortlisted, Selected or Rejected.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
