using FluentAssertions;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Domain.Exceptions;

namespace MinimalCleanArch.UnitTests.Common;

public class ResultErrorTests
{
    [Fact]
    public void ValidationError_ShouldIncludeStatusCodeAndFieldMetadata()
    {
        var error = Error.Validation("TITLE_REQUIRED", "Title is required", "Title");

        error.Type.Should().Be(ErrorType.Validation);
        error.StatusCode.Should().Be(400);
        error.Metadata.Should().ContainKey("Field");
        error.Metadata["Field"].Should().Be("Title");
    }

    [Fact]
    public void WithMetadata_ShouldReturnNewErrorWithoutMutatingOriginal()
    {
        var original = Error.NotFound("TODO_NOT_FOUND", "Todo not found");
        var updated = original.WithMetadata("TodoId", 42);

        original.Metadata.Should().NotContainKey("TodoId");
        updated.Metadata.Should().ContainKey("TodoId");
        updated.Metadata["TodoId"].Should().Be(42);
    }

    [Fact]
    public void Match_ShouldReturnExpectedBranch()
    {
        var success = Result.Success();
        var failure = Result.Failure(Error.Conflict("CONFLICT", "Already exists"));

        success.Match(() => "ok", _ => "fail").Should().Be("ok");
        failure.Match(() => "ok", _ => "fail").Should().Be("fail");
    }

    [Fact]
    public void GenericResult_ShouldConvertToNonGenericResult()
    {
        var success = Result.Success(123);
        var nonGenericSuccess = success.ToResult();

        var failure = Result.Failure<int>(Error.NotFound("NOT_FOUND", "Missing"));
        var nonGenericFailure = failure.ToResult();

        nonGenericSuccess.IsSuccess.Should().BeTrue();
        nonGenericFailure.IsFailure.Should().BeTrue();
        nonGenericFailure.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void DomainException_ShouldCarryStructuredError()
    {
        var ex = new DomainException(Error.BusinessRule("RULE_VIOLATION", "Rule failed"));

        ex.Error.Code.Should().Be("RULE_VIOLATION");
        ex.Error.Type.Should().Be(ErrorType.BusinessRule);
        ex.Error.StatusCode.Should().Be(422);
    }
}
