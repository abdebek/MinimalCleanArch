using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalCleanArch.Validation.Filters;

/// <summary>
/// Filter for validating request bodies using FluentValidation
/// </summary>
/// <typeparam name="TRequest">The type of the request to validate</typeparam>
public class ValidationFilter<TRequest>
{
    /// <summary>
    /// Invokes the validation filter
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <param name="context">The HTTP context</param>
    /// <param name="next">The next delegate in the pipeline</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(
        TRequest request,
        HttpContext context,
        Func<Task> next)
    {
        if (request == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                errors = new[] { "Request body is empty or invalid" }
            });
            return;
        }

        var validator = context.RequestServices.GetService<IValidator<TRequest>>();
        if (validator == null)
        {
            // No validator found, continue
            await next();
            return;
        }

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            await HandleValidationFailureAsync(context, validationResult);
            return;
        }

        await next();
    }

    private static async Task HandleValidationFailureAsync(
        HttpContext context,
        ValidationResult validationResult)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray()
        });
    }
}
