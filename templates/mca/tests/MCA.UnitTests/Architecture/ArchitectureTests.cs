using FluentAssertions;
using NetArchTest.Rules;
using MCA.Domain.Entities;
using MCA.Application.DTOs;
using MCA.Infrastructure.Data;
using Xunit;

namespace MCA.UnitTests.Architecture;

public class ArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var infraResult = Types.InAssembly(typeof(Todo).Assembly)
            .That()
            .ResideInNamespace("MCA.Domain")
            .Should()
            .NotHaveDependencyOn("MCA.Infrastructure")
            .GetResult();

        infraResult.IsSuccessful.Should().BeTrue();

        var endpointsResult = Types.InAssembly(typeof(Todo).Assembly)
            .That()
            .ResideInNamespace("MCA.Domain")
            .Should()
            .NotHaveDependencyOn("MCA.Endpoints")
            .GetResult();

        endpointsResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var assembly = typeof(TodoResponse).Assembly;
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace("MCA.Application")
            .Should()
            .NotHaveDependencyOn("MCA.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(typeof(AppDbContext).Assembly)
            .That()
            .ResideInNamespace("MCA.Infrastructure")
            .Should()
            .NotHaveDependencyOn("MCA.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();

        var endpointResult = Types.InAssembly(typeof(AppDbContext).Assembly)
            .That()
            .ResideInNamespace("MCA.Infrastructure")
            .Should()
            .NotHaveDependencyOn("MCA.Endpoints")
            .GetResult();

        endpointResult.IsSuccessful.Should().BeTrue();
    }
}
