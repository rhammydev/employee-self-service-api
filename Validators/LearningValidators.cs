using EmployeeSelfServiceAPI.DTOs.Learning;
using FluentValidation;

namespace EmployeeSelfServiceAPI.Validators;

public class SaveCourseRequestValidator : AbstractValidator<SaveCourseRequest>
{
    public SaveCourseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.PassMark).InclusiveBetween(1, 100).WithMessage("Pass mark must be between 1 and 100.");
    }
}

public class SaveLessonRequestValidator : AbstractValidator<SaveLessonRequest>
{
    public SaveLessonRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).MaximumLength(20000);
        RuleFor(x => x.MaterialUrl!).Must(BeHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.MaterialUrl))
            .WithMessage("Material URL must be an absolute http(s) URL.");
        RuleFor(x => x.VideoUrl!).Must(BeHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.VideoUrl))
            .WithMessage("Video URL must be an absolute http(s) URL.");
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Content)
                                  || !string.IsNullOrWhiteSpace(x.MaterialUrl)
                                  || !string.IsNullOrWhiteSpace(x.VideoUrl))
            .WithName("content").WithMessage("A lesson needs content, a material URL or a video URL.");
    }

    private static bool BeHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}

public class SaveQuestionRequestValidator : AbstractValidator<SaveQuestionRequest>
{
    public SaveQuestionRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Options).Must(o => o.Count >= 2).WithMessage("A question needs at least two options.");
        RuleFor(x => x.Options).Must(o => o.Count(opt => opt.IsCorrect) == 1)
            .WithMessage("A question must have exactly one correct option.");
        RuleForEach(x => x.Options).ChildRules(option =>
            option.RuleFor(o => o.Text).NotEmpty().MaximumLength(500));
    }
}

public class ReorderRequestValidator : AbstractValidator<ReorderRequest>
{
    public ReorderRequestValidator()
    {
        RuleFor(x => x.Ids).NotEmpty();
        RuleFor(x => x.Ids).Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Ids must not repeat.");
    }
}

public class AssignCourseRequestValidator : AbstractValidator<AssignCourseRequest>
{
    public AssignCourseRequestValidator()
    {
        RuleFor(x => x.EmployeeIds).NotEmpty().WithMessage("At least one employee is required.");
        RuleFor(x => x.EmployeeIds).Must(ids => ids.Count <= 100).WithMessage("Assign at most 100 employees at a time.");
        RuleFor(x => x.DueDate).NotEmpty();
    }
}

public class SubmitAttemptRequestValidator : AbstractValidator<SubmitAttemptRequest>
{
    public SubmitAttemptRequestValidator()
    {
        RuleFor(x => x.Answers).NotEmpty().WithMessage("Answers are required.");
        RuleFor(x => x.Answers).Must(a => a.Select(x => x.QuestionId).Distinct().Count() == a.Count)
            .WithMessage("Each question can be answered only once.");
    }
}
