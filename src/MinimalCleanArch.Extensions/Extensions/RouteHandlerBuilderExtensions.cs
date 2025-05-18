using System;
using System.Linq;
using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Extensions.Models;

namespace MinimalCleanArch.Extensions.Extensions;

/// <summary>
/// Extension methods for <see cref="RouteHandlerBuilder"/>
/// </summary>
public static class RouteHandlerBuilderExtensions
{
    /// <summary>
    /// Adds validation for the request body of a Minimal API endpoint
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body</typeparam>
    /// <param name="builder">The route handler builder</param>
    /// <returns>The route handler builder</returns>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
            if (validator == null)
            {
                return await next(context);
            }

            // Find the parameter of type TRequest
            var parameter = context.Arguments.OfType<TRequest>().FirstOrDefault();
            if (parameter == null)
            {
                return await next(context);
            }

            var validationResult = await validator.ValidateAsync(parameter);
            if (!validationResult.IsValid)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Failed",
                    Type = "https://httpstatuses.com/400",
                    Detail = "One or more validation errors occurred",
                    Instance = context.HttpContext.Request.Path
                };
                
                problemDetails.Extensions["errors"] = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                
                return Results.BadRequest(problemDetails);
            }

            return await next(context);
        });

        return builder;
    }

    /// <summary>
    /// Adds validation for a path parameter of a Minimal API endpoint
    /// </summary>
    /// <typeparam name="T">The type of the path parameter</typeparam>
    /// <param name="builder">The route handler builder</param>
    /// <returns>The route handler builder</returns>
    public static RouteHandlerBuilder WithPathParamValidation<T>(this RouteHandlerBuilder builder)
        where T : class
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
            if (validator == null)
            {
                return await next(context);
            }

            // Find the parameter of type T
            var parameter = context.Arguments.OfType<T>().FirstOrDefault();
            if (parameter == null)
            {
                return await next(context);
            }

            var validationResult = await validator.ValidateAsync(parameter);
            if (!validationResult.IsValid)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Failed",
                    Type = "https://httpstatuses.com/400",
                    Detail = "One or more validation errors occurred",
                    Instance = context.HttpContext.Request.Path
                };
                
                problemDetails.Extensions["errors"] = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                
                return Results.BadRequest(problemDetails);
            }

            return await next(context);
        });

        return builder;
    }

    /// <summary>
    /// Adds error handling to a Minimal API endpoint
    /// </summary>
    /// <param name="builder">The route handler builder</param>
    /// <returns>The route handler builder</returns>
    public static RouteHandlerBuilder WithErrorHandling(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            try
            {
                return await next(context);
            }
            catch (Exception ex)
            {
                var problemDetails = new ProblemDetails();
                
                // Set status code and details based on the exception type
                problemDetails.Status = ex switch
                {
                    ArgumentException => StatusCodes.Status400BadRequest,
                    InvalidOperationException => StatusCodes.Status400BadRequest,
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status500InternalServerError
                };
                
                problemDetails.Title = ex switch
                {
                    ArgumentException => "Bad Request",
                    InvalidOperationException => "Bad Request",
                    UnauthorizedAccessException => "Unauthorized",
                    KeyNotFoundException => "Not Found",
                    _ => "Internal Server Error"
                };
                
                problemDetails.Type = $"https://httpstatuses.com/{problemDetails.Status}";
                problemDetails.Detail = ex.Message;
                problemDetails.Instance = context.HttpContext.Request.Path;
                
                return Results.Problem(
                    title: problemDetails.Title,
                    detail: problemDetails.Detail,
                    statusCode: problemDetails.Status,
                    type: problemDetails.Type,
                    instance: problemDetails.Instance);
            }
        });

        return builder;
    }

    /// <summary>
    /// Adds standard OpenAPI responses to a Minimal API endpoint
    /// </summary>
    /// <typeparam name="TResponse">The type of the response</typeparam>
    /// <param name="builder">The route handler builder</param>
    /// <returns>The route handler builder</returns>
    public static RouteHandlerBuilder WithStandardResponses<TResponse>(this RouteHandlerBuilder builder)
        where TResponse : class
    {
        builder.Produces<TResponse>(StatusCodes.Status200OK);
        builder.ProducesProblem(StatusCodes.Status400BadRequest);
        builder.ProducesProblem(StatusCodes.Status404NotFound);
        builder.ProducesProblem(StatusCodes.Status500InternalServerError);
        
        return builder;
    }
}
