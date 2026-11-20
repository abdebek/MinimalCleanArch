using FluentValidation;
using MinimalCleanArch.Sample.API.Models;

namespace MinimalCleanArch.Sample.API.Validators;

/// <summary>
/// Validator for <see cref="RegisterUserRequest"/>.
/// </summary>
public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name cannot be longer than 200 characters");
    }
}

/// <summary>
/// Validator for <see cref="LoginUserRequest"/>.
/// </summary>
public class LoginUserRequestValidator : AbstractValidator<LoginUserRequest>
{
    public LoginUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

/// <summary>
/// Validator for <see cref="UpdateUserProfileRequest"/>.
/// </summary>
public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("Full name cannot be longer than 200 characters")
            .When(x => x.FullName is not null);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage("Phone number cannot be longer than 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.PersonalNotes)
            .MaximumLength(1000).WithMessage("Personal notes cannot be longer than 1000 characters")
            .When(x => x.PersonalNotes is not null);
    }
}
