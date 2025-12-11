using FluentValidation;
using MCA.Application.Commands;

namespace MCA.Application.Validation;

public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Priority).InclusiveBetween(0, 10);
    }
}

public class UpdateTodoCommandValidator : AbstractValidator<UpdateTodoCommand>
{
    public UpdateTodoCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Priority).InclusiveBetween(0, 10);
    }
}

public class CompleteTodoCommandValidator : AbstractValidator<CompleteTodoCommand>
{
    public CompleteTodoCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class DeleteTodoCommandValidator : AbstractValidator<DeleteTodoCommand>
{
    public DeleteTodoCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class GetTodoByIdQueryValidator : AbstractValidator<GetTodoByIdQuery>
{
    public GetTodoByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
