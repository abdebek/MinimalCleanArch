using System;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Extensions.Errors;

namespace MinimalCleanArch.Extensions.Extensions;

/// <summary>
/// Extension methods for <see cref="RouteHandlerBuilder"/>
/// </summary>
public static class RouteHandlerBuilderExtensions
{
    /// <summary>
    /// Adds validation for the request body of a Minimal API endpoint.
    /// </summary>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
            if (validator is null)
            {
                return await next(context);
            }

            var parameter = context.Arguments.OfType<TRequest>().FirstOrDefault();
            if (parameter is null)
            {
                return await next(context);
            }

            var validationResult = await validator.ValidateAsync(parameter);
            if (validationResult.IsValid)
            {
                return await next(context);
            }

            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problemDetails = MinimalCleanArchProblemDetailsFactory.CreateValidation(
                context.HttpContext,
                errors);

            return Results.ValidationProblem(
                problemDetails.Errors,
                detail: problemDetails.Detail,
                instance: problemDetails.Instance,
                statusCode: problemDetails.Status,
                title: problemDetails.Title,
                type: problemDetails.Type,
                extensions: problemDetails.Extensions.Count > 0 ? problemDetails.Extensions : null);
        });

        return builder;
    }

    /// <summary>
    /// Adds validation for a path parameter of a Minimal API endpoint.
    /// </summary>
    public static RouteHandlerBuilder WithPathParamValidation<T>(this RouteHandlerBuilder builder)
        where T : class
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
            if (validator is null)
            {
                return await next(context);
            }

            var parameter = context.Arguments.OfType<T>().FirstOrDefault();
            if (parameter is null)
            {
                return await next(context);
            }

            var validationResult = await validator.ValidateAsync(parameter);
            if (validationResult.IsValid)
            {
                return await next(context);
            }

            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problemDetails = MinimalCleanArchProblemDetailsFactory.CreateValidation(
                context.HttpContext,
                errors);

            return Results.ValidationProblem(
                problemDetails.Errors,
                detail: problemDetails.Detail,
                instance: problemDetails.Instance,
                statusCode: problemDetails.Status,
                title: problemDetails.Title,
                type: problemDetails.Type,
                extensions: problemDetails.Extensions.Count > 0 ? problemDetails.Extensions : null);
        });

        return builder;
    }

    /// <summary>
    /// Adds error handling to a Minimal API endpoint.
    /// </summary>
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
                var problemDetails = MinimalCleanArchProblemDetailsFactory.CreateForException(
                    context.HttpContext,
                    ex,
                    includeSensitiveDetails: false);

                return Results.Problem(
                    title: problemDetails.Title,
                    detail: problemDetails.Detail,
                    statusCode: problemDetails.Status,
                    type: problemDetails.Type,
                    instance: problemDetails.Instance,
                    extensions: problemDetails.Extensions.Count > 0 ? problemDetails.Extensions : null);
            }
        });

        return builder;
    }

    /// <summary>
    /// Adds standard OpenAPI responses to a Minimal API endpoint.
    /// </summary>
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
