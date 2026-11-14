using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Repositories;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.UnitTests.DataAccess;

/// <summary>
/// Tests for 3-state specification tracking semantics (<see cref="QueryTrackingMode"/>):
/// <list type="bullet">
/// <item><description><see cref="QueryTrackingMode.Default"/> follows the host's <c>UseQueryTrackingBehavior(...)</c> setting.</description></item>
/// <item><description><see cref="QueryTrackingMode.NoTracking"/> explicitly disables tracking, even on a tracking-on host.</description></item>
/// <item><description><see cref="QueryTrackingMode.Tracking"/> explicitly enables tracking, even on a global no-tracking host.</description></item>
/// </list>
/// Count-only specifications default to <see cref="QueryTrackingMode.NoTracking"/> unless <c>UseTracking()</c> is called.
/// </summary>
public class GlobalNoTrackingTests
{
    [Fact]
    public async Task Spec_WithoutTrackingOverride_FollowsHostNoTrackingDefault()
    {
        var dbName = Guid.NewGuid().ToString("N");

        await using (var seed = CreateContext(dbName))
        {
            seed.Notes.Add(new Note { Title = "persisted" });
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = new Repository<Note, int>(context);

        var result = await repository.GetFirstAsync(new ByTitleSpec("persisted"));

        result.Should().NotBeNull();
        context.ChangeTracker.Entries<Note>().Should().BeEmpty();
    }

    [Fact]
    public async Task Spec_WithUseTracking_ReturnsTrackedEntityAndPersistsMutations_UnderGlobalNoTracking()
    {
        var dbName = Guid.NewGuid().ToString("N");

        await using (var seed = CreateContext(dbName))
        {
            seed.Notes.Add(new Note { Title = "initial" });
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext(dbName))
        {
            var repository = new Repository<Note, int>(context);

            var fetched = await repository.GetFirstAsync(new TrackedByTitleSpec("initial"));

            fetched.Should().NotBeNull();
            context.ChangeTracker.Entries<Note>().Should().ContainSingle();

            fetched!.Title = "updated";
            await context.SaveChangesAsync();
        }

        await using var verify = CreateContext(dbName);
        var saved = await verify.Notes.SingleAsync();
        saved.Title.Should().Be("updated");
    }

    [Fact]
    public async Task Spec_WithUseNoTracking_ReturnsDetachedEntity_UnderTrackingDefault()
    {
        var dbName = Guid.NewGuid().ToString("N");

        await using (var seed = CreateContext(dbName, globalNoTracking: false))
        {
            seed.Notes.Add(new Note { Title = "readonly" });
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, globalNoTracking: false);
        var repository = new Repository<Note, int>(context);

        var result = await repository.GetFirstAsync(new ReadOnlyByTitleSpec("readonly"));

        result.Should().NotBeNull();
        context.ChangeTracker.Entries<Note>().Should().BeEmpty();
    }

    [Fact]
    public async Task Spec_WithoutTrackingOverride_ReturnsTrackedEntityAndPersistsMutations_UnderTrackingDefault()
    {
        var dbName = Guid.NewGuid().ToString("N");

        await using (var seed = CreateContext(dbName, globalNoTracking: false))
        {
            seed.Notes.Add(new Note { Title = "default" });
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, globalNoTracking: false);
        var repository = new Repository<Note, int>(context);

        var fetched = await repository.GetFirstAsync(new ByTitleSpec("default"));

        fetched.Should().NotBeNull();
        context.ChangeTracker.Entries<Note>().Should().ContainSingle();

        fetched!.Title = "updated";
        await context.SaveChangesAsync();

        await using var verify = CreateContext(dbName, globalNoTracking: false);
        var saved = await verify.Notes.SingleAsync();
        saved.Title.Should().Be("updated");
    }

    [Fact]
    public async Task CountOnlySpec_MaterializesNothing_UnderTrackingDefault()
    {
        var dbName = Guid.NewGuid().ToString("N");

        await using (var seed = CreateContext(dbName, globalNoTracking: false))
        {
            seed.Notes.Add(new Note { Title = "counted" });
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, globalNoTracking: false);
        var repository = new Repository<Note, int>(context);

        var count = await repository.CountAsync(new CountOnlyByTitleSpec("counted"));

        count.Should().Be(1);
        context.ChangeTracker.Entries<Note>().Should().BeEmpty();
    }

    [Fact]
    public void TrackingMode_And_AsNoTracking_ReflectSpecificationConfiguration()
    {
        var defaultSpec = new ByTitleSpec("any");
        defaultSpec.TrackingMode.Should().Be(QueryTrackingMode.Default);
        defaultSpec.AsNoTracking.Should().BeFalse();

        var noTrackingSpec = new ReadOnlyByTitleSpec("any");
        noTrackingSpec.TrackingMode.Should().Be(QueryTrackingMode.NoTracking);
        noTrackingSpec.AsNoTracking.Should().BeTrue();

        var trackingSpec = new TrackedByTitleSpec("any");
        trackingSpec.TrackingMode.Should().Be(QueryTrackingMode.Tracking);
        trackingSpec.AsNoTracking.Should().BeFalse();

        var countOnlySpec = new CountOnlyByTitleSpec("any");
        countOnlySpec.TrackingMode.Should().Be(QueryTrackingMode.NoTracking);
        countOnlySpec.AsNoTracking.Should().BeTrue();

        var trackedCountOnlySpec = new TrackedCountOnlyByTitleSpec("any");
        trackedCountOnlySpec.TrackingMode.Should().Be(QueryTrackingMode.Tracking);
        trackedCountOnlySpec.AsNoTracking.Should().BeFalse();
    }

    [Fact]
    public async Task Repository_AdHocReadHelpers_RouteThroughApplySpecification()
    {
        var dbName = Guid.NewGuid().ToString("N");
        int seededId;

        await using (var seed = CreateContext(dbName))
        {
            var note = new Note { Title = "alpha" };
            seed.Notes.Add(note);
            await seed.SaveChangesAsync();
            seededId = note.Id;
        }

        await using var context = CreateContext(dbName);
        var repository = new SpyNoteRepository(context);

        (await repository.GetAllAsync()).Should().ContainSingle();
        (await repository.GetAsync(n => n.Title == "alpha")).Should().ContainSingle();
        (await repository.AnyAsync(n => n.Title == "alpha")).Should().BeTrue();
        (await repository.GetByIdAsync(seededId)).Should().NotBeNull();
        (await repository.GetFirstAsync(n => n.Title == "alpha")).Should().NotBeNull();
        (await repository.SingleOrDefaultAsync(n => n.Title == "alpha")).Should().NotBeNull();
        (await repository.CountAsync(n => n.Title == "alpha")).Should().Be(1);

        repository.ApplySpecificationCalls.Should().Be(7);
    }

    private static TestNoteDbContext CreateContext(string dbName, bool globalNoTracking = true)
    {
        var builder = new DbContextOptionsBuilder<TestNoteDbContext>()
            .UseInMemoryDatabase(dbName);

        if (globalNoTracking)
        {
            builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        return new TestNoteDbContext(builder.Options);
    }

    private sealed class TestNoteDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Note> Notes => Set<Note>();
    }

    private sealed class SpyNoteRepository(DbContext dbContext) : Repository<Note, int>(dbContext)
    {
        public int ApplySpecificationCalls { get; private set; }

        protected override IQueryable<Note> ApplySpecification(ISpecification<Note> specification)
        {
            ApplySpecificationCalls++;
            return base.ApplySpecification(specification);
        }
    }

    private sealed class Note : BaseSoftDeleteEntity
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class ByTitleSpec : BaseSpecification<Note>
    {
        public ByTitleSpec(string title) : base(n => n.Title == title)
        {
        }
    }

    private sealed class ReadOnlyByTitleSpec : BaseSpecification<Note>
    {
        public ReadOnlyByTitleSpec(string title) : base(n => n.Title == title)
        {
            UseNoTracking();
        }
    }

    private sealed class TrackedByTitleSpec : BaseSpecification<Note>
    {
        public TrackedByTitleSpec(string title) : base(n => n.Title == title)
        {
            UseTracking();
        }
    }

    private sealed class CountOnlyByTitleSpec : BaseSpecification<Note>
    {
        public CountOnlyByTitleSpec(string title) : base(n => n.Title == title)
        {
            ForCountOnly();
        }
    }

    private sealed class TrackedCountOnlyByTitleSpec : BaseSpecification<Note>
    {
        public TrackedCountOnlyByTitleSpec(string title) : base(n => n.Title == title)
        {
            ForCountOnly();
            UseTracking();
        }
    }
}
