using EmployeeSelfServiceAPI.DTOs.Payroll;
using FluentValidation;

namespace EmployeeSelfServiceAPI.Validators;

public class PayslipLineRequestValidator : AbstractValidator<PayslipLineRequest>
{
    public PayslipLineRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).WithMessage("Amounts cannot be negative.")
            .PrecisionScale(18, 2, true).WithMessage("Amounts can have at most 2 decimal places.");
    }
}

public class CreatePayslipRequestValidator : AbstractValidator<CreatePayslipRequest>
{
    public CreatePayslipRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        Include(new PayslipAmountsRules());
    }
}

public class CreateRevisionRequestValidator : AbstractValidator<CreateRevisionRequest>
{
    public CreateRevisionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required for a revision.").MaximumLength(1000);
        Include(new PayslipAmountsRules());
    }
}

public class PayslipAmountsRules : AbstractValidator<PayslipAmountsRequest>
{
    public PayslipAmountsRules()
    {
        RuleFor(x => x.BasicSalary).GreaterThanOrEqualTo(0).WithMessage("Basic salary cannot be negative.")
            .PrecisionScale(18, 2, true).WithMessage("Amounts can have at most 2 decimal places.");
        RuleForEach(x => x.Allowances).SetValidator(new PayslipLineRequestValidator());
        RuleForEach(x => x.Deductions).SetValidator(new PayslipLineRequestValidator());
    }
}
