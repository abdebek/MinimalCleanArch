using Microsoft.AspNetCore.Http;
using MinimalCleanArch.Extensions.Errors;
using MinimalCleanArch.Domain.Common;

namespace MinimalCleanArch.Extensions.Extensions;

/// <summary>
/// Helpers for mapping <see cref="Result"/> and <see cref="Error"/> to Minimal API <see cref="IResult"/>.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Converts an <see cref="Error"/> to RFC 7807 response payload.
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
    /// Maps a non-generic result to <see cref="IResult"/> using the provided success branch.
    /// </summary>
    public static IResult MatchHttp(
        this Result result,
        HttpContext context,
        Func<IResult> onSuccess,
        string? instance = null,
        bool includeSensitiveDetails = false)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess
            ? onSuccess()
            : result.Error.ToProblem(context, instance, includeSensitiveDetails);
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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess
            ? onSuccess(result.Value)
            : result.Error.ToProblem(context, instance, includeSensitiveDetails);
    }
}
