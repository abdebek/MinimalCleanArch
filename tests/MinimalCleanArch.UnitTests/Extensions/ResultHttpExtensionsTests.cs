using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Extensions.Extensions;

namespace MinimalCleanArch.UnitTests.Extensions;

public class ResultHttpExtensionsTests
{
    [Fact]
    public async Task ToProblem_MapsStructuredErrorToProblemDetails()
    {
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-123";
        context.Items["CorrelationId"] = "corr-456";

        var error = Error.NotFound("TODO_NOT_FOUND", "Todo not found")
            .WithMetadata("TodoId", 42);

        var result = error.ToProblem(context);
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;

        root.GetProperty("title").GetString().Should().Be("Not Found");
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        root.GetProperty("code").GetString().Should().Be("TODO_NOT_FOUND");
        root.GetProperty("errorType").GetString().Should().Be(nameof(ErrorType.NotFound));
        root.GetProperty("metadata").GetProperty("TodoId").GetInt32().Should().Be(42);
        root.GetProperty("traceId").GetString().Should().Be("trace-123");
        root.GetProperty("correlationId").GetString().Should().Be("corr-456");
    }

    [Fact]
    public async Task ToProblem_RedactsSensitiveMetadataInProduction()
    {
        var context = CreateHttpContext();
        var error = Error.SystemError("BOOM", "Internal details")
            .WithMetadata("StackTrace", "at Secret.Method()")
            .WithMetadata("ExceptionType", "SqlException")
            .WithMetadata("PublicKey", "safe-value");

        var result = error.ToProblem(context, includeSensitiveDetails: false);
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;

        root.GetProperty("detail").GetString().Should().Be("An unexpected error occurred. Please try again later.");
        var metadata = root.GetProperty("metadata");
        metadata.TryGetProperty("StackTrace", out _).Should().BeFalse();
        metadata.TryGetProperty("ExceptionType", out _).Should().BeFalse();
        metadata.GetProperty("PublicKey").GetString().Should().Be("safe-value");
    }

    [Fact]
    public async Task MatchHttp_WhenFailure_ReturnsProblem()
    {
        var context = CreateHttpContext();

        var result = Result.Failure<int>(Error.Validation("INVALID", "Invalid input"));
        var httpResult = result.MatchHttp(context, value => Results.Ok(value));

        await httpResult.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task MatchHttp_WhenSuccess_UsesSuccessBranch()
    {
        var context = CreateHttpContext();

        var result = Result.Success(123);
        var httpResult = result.MatchHttp(context, value => Results.Ok(new { id = value }));

        await httpResult.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void MatchActionResult_WhenFailure_ReturnsProblemDetailsObjectResult()
    {
        var context = CreateHttpContext();
        Result<int> result = Error.NotFound("TODO_NOT_FOUND", "Todo 7 not found");

        var actionResult = result.MatchActionResult(context, v => new OkObjectResult(v));

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        objectResult.ContentTypes.Should().Contain("application/problem+json");
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be("TODO_NOT_FOUND");
    }

    [Fact]
    public async Task WriteProblemAsync_WritesRfc7807ProblemDetails()
    {
        var context = CreateHttpContext();
        var error = Error.Conflict("DUPLICATE", "Already exists");

        await error.WriteProblemAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        root.GetProperty("code").GetString().Should().Be("DUPLICATE");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        context.Response.Body = new MemoryStream();
        return context;
    }
}
