using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Extensions.Errors;

namespace MinimalCleanArch.Extensions.Extensions;

/// <summary>
/// Helpers for mapping <see cref="Result"/>, <see cref="Result{TValue}"/>, and <see cref="Error"/>
/// to RFC 7807 Problem Details across Minimal APIs, MVC Controllers, and direct <see cref="HttpContext"/> hosts (e.g. FastEndpoints).
/// </summary>
public static class ResultHttpExtensions
{
    private static readonly JsonSerializerOptions ProblemJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Converts an <see cref="Error"/> to an RFC 7807 Minimal API <see cref="IResult"/>.
    /// </summary>
    public static IResult ToProblem(
        this Error error,
        HttpContext context,
        string? instance = null,
        bool includeSensitiveDetails = false)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(context);

        var problemDetails = MinimalCleanArchProblemDetailsFactory.CreateForError(
            context,
            error,
            includeSensitiveDetails,
            instance);

        return Results.Problem(
            title: problemDetails.Title,
            detail: problemDetails.Detail,
            statusCode: problemDetails.Status,
            type: problemDetails.Type,
            instance: problemDetails.Instance,
            extensions: problemDetails.Extensions.Count > 0 ? problemDetails.Extensions : null);
    }

    /// <summary>
    /// Converts an <see cref="Error"/> to an RFC 7807 MVC <see cref="IActionResult"/>.
    /// </summary>
    public static IActionResult ToActionResult(
        this Error error,
        HttpContext context,
        string? instance = null,
        bool includeSensitiveDetails = false)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(context);

        var problemDetails = MinimalCleanArchProblemDetailsFactory.CreateForError(
            context,
            error,
            includeSensitiveDetails,
            instance);

        var result = new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    /// <summary>
    /// Writes an <see cref="Error"/> directly to the HTTP response as RFC 7807 Problem Details.
    /// Useful for host adapters such as FastEndpoints that write directly to <see cref="HttpContext"/>.
    /// </summary>
    public static async Task WriteProblemAsync(
        this Error error,
        HttpContext context,
        CancellationToken cancellationToken = default,
        string? instance = null,
        bool includeSensitiveDetails = false)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(context);

        var problemDetails = MinimalCleanArchProblemDetailsFactory.CreateForError(
            context,
            error,
            includeSensitiveDetails,
            instance);

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        if (context.RequestServices?.GetService(typeof(IProblemDetailsService)) is IProblemDetailsService problemDetailsService)
        {
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problemDetails
            });
            return;
        }

        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails, ProblemJsonOptions, cancellationToken);
    }

    /// <summary>
    /// Maps a non-generic result to <see cref="IResult"/> using the provided success branch.
    /// </summary>
    public static IResult MatchHttp(
        this Result result,
        HttpContext context,
        Func<IResult> onSuccess,
        string? instance = null,
        bool includeSensitiveDetails = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result switch
        {
            Success => onSuccess(),
            Error error => error.ToProblem(context, instance, includeSensitiveDetails)
        };
    }

    /// <summary>
    /// Maps a generic result to <see cref="IResult"/> using the provided success branch.
    /// </summary>
    public static IResult MatchHttp<TValue>(
        this Result<TValue> result,
        HttpContext context,
        Func<TValue, IResult> onSuccess,
        string? instance = null,
        bool includeSensitiveDetails = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result switch
        {
            TValue value => onSuccess(value),
            Error error => error.ToProblem(context, instance, includeSensitiveDetails),
            null => onSuccess(default!)
        };
    }

    /// <summary>
    /// Maps a non-generic result to an MVC <see cref="IActionResult"/> using the provided success branch.
    /// </summary>
    public static IActionResult MatchActionResult(
        this Result result,
        HttpContext context,
        Func<IActionResult> onSuccess,
        string? instance = null,
        bool includeSensitiveDetails = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result switch
        {
            Success => onSuccess(),
            Error error => error.ToActionResult(context, instance, includeSensitiveDetails)
        };
    }

    /// <summary>
    /// Maps a generic result to an MVC <see cref="IActionResult"/> using the provided success branch.
    /// </summary>
    public static IActionResult MatchActionResult<TValue>(
        this Result<TValue> result,
        HttpContext context,
        Func<TValue, IActionResult> onSuccess,
        string? instance = null,
        bool includeSensitiveDetails = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result switch
        {
            TValue value => onSuccess(value),
            Error error => error.ToActionResult(context, instance, includeSensitiveDetails),
            null => onSuccess(default!)
        };
    }
}
