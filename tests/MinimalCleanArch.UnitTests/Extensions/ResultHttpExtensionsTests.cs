using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
