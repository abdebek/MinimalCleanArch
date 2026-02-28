using Microsoft.AspNetCore.Http;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Extensions.Errors;

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

        var statusCode = ErrorResponseMapper.ResolveStatusCode(error);
        var title = ErrorResponseMapper.ResolveTitle(error);
        var detail = ErrorResponseMapper.ResolveDetail(error, includeSensitiveDetails);
        var extensions = ErrorResponseMapper.CreateBaseExtensions(context, error);

        return Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            type: $"https://httpstatuses.com/{statusCode}",
            instance: instance ?? context.Request.Path,
            extensions: extensions.Count > 0 ? extensions : null);
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
