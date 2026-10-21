using EmployeeSelfServiceAPI.DTOs.Employee;
using FluentValidation;

namespace EmployeeSelfServiceAPI.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeNumber)
            .NotEmpty().WithMessage("Employee number is required.")
            .MaximumLength(20).WithMessage("Employee number must not exceed 20 characters.")
            .Matches(@"^[A-Za-z0-9-]+$").WithMessage("Employee number may contain only letters, digits and '-'.");
        RuleFor(x => x.FirstName).ValidName();
        RuleFor(x => x.LastName).ValidName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.PhoneNumber!).ValidPhone().When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Department is required.");
        RuleFor(x => x.GradeId).GreaterThan(0).WithMessage("Grade is required.");
        RuleFor(x => x.ReportingManagerId).GreaterThan(0).When(x => x.ReportingManagerId.HasValue);
    }
}

public class UpdateEmploymentRequestValidator : AbstractValidator<UpdateEmploymentRequest>
{
    public UpdateEmploymentRequestValidator()
    {
        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Department is required.");
        RuleFor(x => x.GradeId).GreaterThan(0).WithMessage("Grade is required.");
        RuleFor(x => x.ReportingManagerId).GreaterThan(0).When(x => x.ReportingManagerId.HasValue);
    }
}

public class UpdateRolesRequestValidator : AbstractValidator<UpdateRolesRequest>
{
    public UpdateRolesRequestValidator()
    {
        RuleFor(x => x.Roles).NotNull().WithMessage("Roles are required.");
        RuleForEach(x => x.Roles).IsInEnum().WithMessage("Invalid role.");
    }
}

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required when deactivating an employee.")
            .When(x => !x.IsActive);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class UpdatePhoneRequestValidator : AbstractValidator<UpdatePhoneRequest>
{
    public UpdatePhoneRequestValidator()
    {
        RuleFor(x => x.PhoneNumber).ValidPhone();
    }
}