using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Extensions.Extensions;

namespace MinimalCleanArch.UnitTests.Extensions;

public class ValidationHttpExtensionsTests
{
    [Fact]
    public async Task ValidateAsync_WhenNoValidatorRegistered_ReturnsNull()
    {
        var context = CreateHttpContext();

        var result = await context.ValidateAsync(new SampleRequest("ok"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WhenValid_ReturnsNull()
    {
        var context = CreateHttpContext(services =>
        {
            services.AddScoped<IValidator<SampleRequest>, SampleRequestValidator>();
        });

        var result = await context.ValidateAsync(new SampleRequest("valid title"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WhenInvalid_ReturnsValidationProblem()
    {
        var context = CreateHttpContext(services =>
        {
            services.AddScoped<IValidator<SampleRequest>, SampleRequestValidator>();
            services.AddProblemDetails();
        });

        var result = await context.ValidateAsync(new SampleRequest(""));
        result.Should().NotBeNull();

        await result!.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    private static DefaultHttpContext CreateHttpContext(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        configure?.Invoke(services);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed record SampleRequest(string Title);

    private sealed class SampleRequestValidator : AbstractValidator<SampleRequest>
    {
        public SampleRequestValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
        }
    }
}
