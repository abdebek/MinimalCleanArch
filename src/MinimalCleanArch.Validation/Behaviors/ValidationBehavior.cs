using FluentValidation;
using FluentValidation.Results;

namespace MinimalCleanArch.Validation.Behaviors;

/// <summary>
/// Validation behavior for request handlers
/// </summary>
/// <typeparam name="TRequest">The type of the request</typeparam>
/// <typeparam name="TResponse">The type of the response</typeparam>
public class ValidationBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class
    /// </summary>
    /// <param name="validators">The validators for the request</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Validates the request
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>The validation result</returns>
    public async Task<ValidationResult> ValidateAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_validators.Any())
        {
            return new ValidationResult();
        }

        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        return new ValidationResult(failures);
    }
}
