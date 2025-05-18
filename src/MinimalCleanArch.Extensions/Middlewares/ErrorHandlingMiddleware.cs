using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using MinimalCleanArch.Extensions.Models;

namespace MinimalCleanArch.Extensions.Middlewares;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next,
                                   ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);          // call the rest of the pipeline
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }



    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        
        var problemDetails = new ProblemDetails
        {
            Status = (int)GetStatusCode(exception),
            Title = GetTitle(exception),
            Type = $"https://httpstatuses.com/{(int)GetStatusCode(exception)}",
            Detail = exception.Message,
            Instance = context.Request.Path
        };
        
        if (Debugger.IsAttached)
        {
            problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace ?? string.Empty;
        }
        
        var statusCode = (int)GetStatusCode(exception);
        context.Response.StatusCode = statusCode;
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }
    
    private static HttpStatusCode GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ApplicationException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };
    }
    
    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            ApplicationException => "Bad Request",
            UnauthorizedAccessException => "Unauthorized",
            KeyNotFoundException => "Not Found",
            _ => "Internal Server Error"
        };
    }
}
