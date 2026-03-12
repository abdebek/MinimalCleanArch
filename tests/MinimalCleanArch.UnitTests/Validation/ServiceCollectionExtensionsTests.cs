using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ValidationServiceCollectionExtensions = MinimalCleanArch.Validation.Extensions.ServiceCollectionExtensions;

namespace MinimalCleanArch.UnitTests.Validation;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddValidationFromAssemblyContaining_RegistersValidators()
    {
        var services = new ServiceCollection();

        ValidationServiceCollectionExtensions.AddValidationFromAssemblyContaining<TestCommandValidator>(services);

        using var provider = services.BuildServiceProvider();

        provider.GetService<IValidator<TestCommand>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMinimalCleanArchValidation_RegistersValidatorsFromSuppliedAssemblies()
    {
        var services = new ServiceCollection();

        ValidationServiceCollectionExtensions.AddMinimalCleanArchValidation(services, typeof(TestCommandValidator).Assembly);

        using var provider = services.BuildServiceProvider();

        provider.GetService<IValidator<TestCommand>>().Should().NotBeNull();
    }

    public sealed record TestCommand(string Name);

    public sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
