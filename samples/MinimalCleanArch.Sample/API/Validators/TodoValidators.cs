using FluentValidation;
using MinimalCleanArch.Sample.API.Models;

namespace MinimalCleanArch.Sample.API.Validators;

/// <summary>
/// Validator for <see cref="CreateTodoRequest"/>
/// </summary>
public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTodoRequestValidator"/> class
    /// </summary>
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(100).WithMessage("Title cannot be longer than 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot be longer than 500 characters");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 5).WithMessage("Priority must be between 0 and 5");

        RuleFor(x => x.DueDate)
            .Must(BeAValidDueDate).WithMessage("Due date must be in the future")
            .When(x => x.DueDate.HasValue);
    }

    private static bool BeAValidDueDate(DateTime? dueDate)
    {
        return !dueDate.HasValue || dueDate.Value.Date >= DateTime.Now.Date;
    }
}

/// <summary>
/// Validator for <see cref="UpdateTodoRequest"/>
/// </summary>
public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTodoRequestValidator"/> class
    /// </summary>
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(100).WithMessage("Title cannot be longer than 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot be longer than 500 characters");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 5).WithMessage("Priority must be between 0 and 5");

        RuleFor(x => x.DueDate)
            .Must(BeAValidDueDate).WithMessage("Due date must be in the future")
            .When(x => x.DueDate.HasValue);
    }

    private static bool BeAValidDueDate(DateTime? dueDate)
    {
        return !dueDate.HasValue || dueDate.Value.Date >= DateTime.Now.Date;
    }
}
