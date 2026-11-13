using System.Linq;
using FluentAssertions;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.UnitTests.Specifications;

public class InMemorySpecificationEvaluatorTests
{
    [Fact]
    public void Evaluate_AppliesCriteriaOrderingThenByAndPaging()
    {
        var source = CreateItems();
        var specification = new OrderedPagedSpecification(minScore: 60, skip: 1, take: 2);

        var results = InMemorySpecificationEvaluator.Evaluate(source, specification);

        results.Select(x => x.Id).Should().Equal(3, 4);
    }

    [Fact]
    public void Evaluate_CountOnly_IgnoresOrderingAndPaging()
    {
        var source = CreateItems();
        var specification = new CountOnlySpecification(minScore: 60, skip: 1, take: 1);

        var results = InMemorySpecificationEvaluator.Evaluate(source, specification);

        results.Select(x => x.Id).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void CompositeSpecifications_CopyQueryFlags()
    {
        var left = new FlagSpecification(asSplitQuery: true, asNoTracking: true);
        var right = new FlagSpecification(ignoreSoftDelete: true, ignoreQueryFilters: true, isCountOnly: true);

        var combined = left.And(right);

        combined.AsSplitQuery.Should().BeTrue();
        combined.AsNoTracking.Should().BeTrue();
        combined.TrackingMode.Should().Be(QueryTrackingMode.NoTracking);
        combined.IgnoreSoftDelete.Should().BeTrue();
        combined.IgnoreQueryFilters.Should().BeTrue();
        combined.IsCountOnly.Should().BeTrue();
    }

    [Fact]
    public void CompositeSpecifications_PreserveThreeStateTrackingMode()
    {
        var defaultSpec = new FlagSpecification();
        var trackingSpec = new FlagSpecification(asTracking: true);
        var noTrackingSpec = new FlagSpecification(asNoTracking: true);
        var trackedCountOnlySpec = new FlagSpecification(asTracking: true, isCountOnly: true);

        defaultSpec.And(defaultSpec).TrackingMode.Should().Be(QueryTrackingMode.Default);
        defaultSpec.And(trackingSpec).TrackingMode.Should().Be(QueryTrackingMode.Tracking);
        defaultSpec.Or(trackingSpec).TrackingMode.Should().Be(QueryTrackingMode.Tracking);
        trackingSpec.And(noTrackingSpec).TrackingMode.Should().Be(QueryTrackingMode.NoTracking);
        trackedCountOnlySpec.And(defaultSpec).TrackingMode.Should().Be(QueryTrackingMode.Tracking);

        defaultSpec.Not().TrackingMode.Should().Be(QueryTrackingMode.Default);
        trackingSpec.Not().TrackingMode.Should().Be(QueryTrackingMode.Tracking);
        noTrackingSpec.Not().TrackingMode.Should().Be(QueryTrackingMode.NoTracking);
        trackedCountOnlySpec.Not().TrackingMode.Should().Be(QueryTrackingMode.Tracking);
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsExpectedResult()
    {
        var specification = new OrderedPagedSpecification(minScore: 80, skip: 0, take: 10);

        specification.IsSatisfiedBy(new TestItem(1, "A", 90)).Should().BeTrue();
        specification.IsSatisfiedBy(new TestItem(2, "A", 70)).Should().BeFalse();
    }

    private static List<TestItem> CreateItems() =>
        new()
        {
            new TestItem(1, "B", 85),
            new TestItem(2, "A", 90),
            new TestItem(3, "A", 70),
            new TestItem(4, "B", 95),
            new TestItem(5, "A", 40)
        };

    private sealed record TestItem(int Id, string Group, int Score);

    private sealed class OrderedPagedSpecification : BaseSpecification<TestItem>
    {
        public OrderedPagedSpecification(int minScore, int skip, int take)
        {
            AddCriteria(x => x.Score >= minScore);
            ApplyOrderBy(x => x.Group);
            ApplyThenByDescending(x => x.Score);
            ApplyPaging(skip, take);
        }
    }

    private sealed class CountOnlySpecification : BaseSpecification<TestItem>
    {
        public CountOnlySpecification(int minScore, int skip, int take)
        {
            AddCriteria(x => x.Score >= minScore);
            ApplyOrderByDescending(x => x.Score);
            ApplyPaging(skip, take);
            ForCountOnly();
        }
    }

    private sealed class FlagSpecification : BaseSpecification<TestItem>
    {
        public FlagSpecification(
            bool asNoTracking = false,
            bool asTracking = false,
            bool asSplitQuery = false,
            bool ignoreQueryFilters = false,
            bool ignoreSoftDelete = false,
            bool isCountOnly = false)
        {
            if (asNoTracking)
            {
                UseNoTracking();
            }
            else if (asTracking)
            {
                UseTracking();
            }

            if (asSplitQuery)
            {
                UseSplitQuery();
            }

            if (ignoreQueryFilters)
            {
                IgnoreAllQueryFilters();
            }

            if (ignoreSoftDelete)
            {
                IgnoreSoftDeleteFilter();
            }

            if (isCountOnly)
            {
                ForCountOnly();
            }
        }
    }
}
