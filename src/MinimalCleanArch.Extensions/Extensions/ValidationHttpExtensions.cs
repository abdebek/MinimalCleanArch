using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Extensions.Errors;

namespace MinimalCleanArch.Extensions.Extensions;

/// <summary>
/// Helpers for validating models constructed inside Minimal API handlers
/// (commands, queries, path-bound types) using registered FluentValidation validators.
/// </summary>
public static class ValidationHttpExtensions
{
    /// <summary>
    /// Validates <paramref name="instance"/> with a registered FluentValidation validator.
    /// Returns an RFC 7807 validation problem when invalid; otherwise <c>null</c>.
    /// When no validator is registered for <typeparamref name="T"/>, returns <c>null</c> (no-op).
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation problem result, or <c>null</c> when valid or no validator is registered.</returns>
    public static async Task<IResult?> ValidateAsync<T>(
        this HttpContext httpContext,
        T instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(instance);

        var validator = httpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return null;
        }

        var validationResult = await validator.ValidateAsync(instance, cancellationToken);
        if (validationResult.IsValid)
        {
            return null;
        }

        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problemDetails = MinimalCleanArchProblemDetailsFactory.CreateValidation(
            httpContext,
            errors);

        return Results.ValidationProblem(
            problemDetails.Errors,
            detail: problemDetails.Detail,
            instance: problemDetails.Instance,
            statusCode: problemDetails.Status,
            title: problemDetails.Title,
            type: problemDetails.Type,
            extensions: problemDetails.Extensions.Count > 0 ? problemDetails.Extensions : null);
    }
}
