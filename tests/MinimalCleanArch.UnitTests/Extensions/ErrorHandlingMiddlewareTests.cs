using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Domain.Exceptions;
using MinimalCleanArch.Extensions.Middlewares;

namespace MinimalCleanArch.UnitTests.Extensions;

public class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenDomainExceptionThrown_UsesStructuredErrorShape()
    {
        var error = Error.NotFound("TODO_NOT_FOUND", "Todo not found")
            .WithMetadata("TodoId", 10);

        RequestDelegate next = _ => throw new DomainException(error);
        var middleware = new ErrorHandlingMiddleware(
            next,
            NullLogger<ErrorHandlingMiddleware>.Instance,
            new TestHostEnvironment { EnvironmentName = Environments.Production });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
        context.Response.ContentType.Should().Contain("json");

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);

        doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        doc.RootElement.GetProperty("code").GetString().Should().Be("TODO_NOT_FOUND");
        doc.RootElement.GetProperty("errorType").GetString().Should().Be(nameof(ErrorType.NotFound));
        doc.RootElement.GetProperty("metadata").GetProperty("TodoId").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task InvokeAsync_WhenDomainExceptionWithSystemErrorInProduction_HidesDetail()
    {
        var error = Error.SystemError("UNEXPECTED", "Sensitive internals");

        RequestDelegate next = _ => throw new DomainException(error);
        var middleware = new ErrorHandlingMiddleware(
            next,
            NullLogger<ErrorHandlingMiddleware>.Instance,
            new TestHostEnvironment { EnvironmentName = Environments.Production });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);

        doc.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        doc.RootElement.GetProperty("detail").GetString()
            .Should().Be("An unexpected error occurred. Please try again later.");
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
