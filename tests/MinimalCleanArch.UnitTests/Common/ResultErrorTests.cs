using FluentAssertions;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Domain.Entities;
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
    public void ErrorMetadata_ShouldBeImmutable()
    {
        var error = Error.NotFound("TODO_NOT_FOUND", "Missing").WithMetadata("Id", 1);

        var act = () => ((IDictionary<string, object>)error.Metadata)["Id"] = 2;

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void FromException_ShouldOmitStackTraceByDefault()
    {
        Exception captured;
        try
        {
            throw new InvalidOperationException("Boom");
        }
        catch (Exception ex)
        {
            captured = ex;
        }

        var defaultError = Error.FromException(captured);
        defaultError.Metadata.Should().NotContainKey("StackTrace");
        defaultError.Metadata.Should().NotContainKey("ExceptionType");

        var detailedError = Error.FromException(captured, includeStackTrace: true);
        detailedError.Metadata.Should().ContainKey("StackTrace");
        detailedError.Metadata.Should().ContainKey("ExceptionType");
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
    public void Result_SupportsExhaustiveCSharp15UnionPatternMatching()
    {
        Result ok = default(Success);
        Result fail = Error.Forbidden("FORBIDDEN", "Denied");
        Result uninitialized = default;

        static string Describe(Result result) => result switch
        {
            Success => "success",
            Error e => e.Code
        };

        Describe(ok).Should().Be("success");
        Describe(fail).Should().Be("FORBIDDEN");
        Describe(uninitialized).Should().Be("RESULT_UNINITIALIZED");
    }

    [Fact]
    public void GenericResult_SupportsExhaustiveCSharp15UnionPatternMatching_ForValueAndReferenceTypes()
    {
        Result<int> okInt = 42;
        Result<int> failInt = Error.NotFound("MISSING", "Not found");
        Result<int> defaultInt = default;

        static string DescribeInt(Result<int> result) => result switch
        {
            int n => $"value:{n}",
            Error e => $"error:{e.Code}"
        };

        DescribeInt(okInt).Should().Be("value:42");
        DescribeInt(failInt).Should().Be("error:MISSING");
        DescribeInt(defaultInt).Should().Be("error:RESULT_UNINITIALIZED");

        Result<string> okString = "hello";
        okString.GetValueOrThrow().Should().Be("hello");
        okString.TryGetValue(out string? value).Should().BeTrue();
        value.Should().Be("hello");

        var throwAct = () => failInt.GetValueOrThrow();
        throwAct.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MapAndBind_ShouldPropagateUnexpectedExceptions()
    {
        Result<int> ok = 10;

        var mapAct = () => ok.Map<int>(_ => throw new InvalidOperationException("unexpected"));
        var bindAct = () => ok.Bind<int>(_ => throw new InvalidOperationException("unexpected"));

        mapAct.Should().Throw<InvalidOperationException>().WithMessage("unexpected");
        bindAct.Should().Throw<InvalidOperationException>().WithMessage("unexpected");
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

    [Fact]
    public void BaseEntity_TransientEntitiesShouldNotBeEqual()
    {
        var first = new SampleEntity();
        var second = new SampleEntity();

        first.Equals(first).Should().BeTrue();
        first.Equals(second).Should().BeFalse();
        (first == second).Should().BeFalse();

        var set = new HashSet<SampleEntity> { first, second };
        set.Should().HaveCount(2);
    }

    [Fact]
    public void BaseEntity_PersistedEntitiesWithSameIdShouldBeEqual()
    {
        var first = new SampleEntity { Id = 42 };
        var second = new SampleEntity { Id = 42 };
        var third = new SampleEntity { Id = 99 };

        first.Equals(second).Should().BeTrue();
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
        first.Equals(third).Should().BeFalse();
    }

    private sealed class SampleEntity : BaseEntity;
}
