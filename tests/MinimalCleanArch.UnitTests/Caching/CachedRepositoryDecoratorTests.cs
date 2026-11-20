using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Execution;
using MinimalCleanArch.Extensions.Caching;
using MinimalCleanArch.Repositories;
using Moq;

namespace MinimalCleanArch.UnitTests.Caching;

public class CachedRepositoryDecoratorTests
{
    [Fact]
    public async Task TenantScopedEntity_IsolatesCachePerTenant()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new MemoryCacheService(memoryCache, NullLogger<MemoryCacheService>.Instance);

        var innerRepo = new Mock<IRepository<TenantNote, int>>();
        var tenantContext = new MutableExecutionContext { TenantId = "tenant-a" };

        innerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new TenantNote { Id = 1, TenantId = tenantContext.TenantId!, Text = $"from-{tenantContext.TenantId}" });

        var decorator = new CachedRepositoryDecorator<TenantNote, int>(innerRepo.Object, cacheService, executionContext: tenantContext);

        var noteA = await decorator.GetByIdAsync(1);
        tenantContext.TenantId = "tenant-b";
        var noteB = await decorator.GetByIdAsync(1);

        noteA!.Text.Should().Be("from-tenant-a");
        noteB!.Text.Should().Be("from-tenant-b");
        innerRepo.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TenantScopedEntity_WithoutTenantId_BypassesCache()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new MemoryCacheService(memoryCache, NullLogger<MemoryCacheService>.Instance);

        var innerRepo = new Mock<IRepository<TenantNote, int>>();
        var tenantContext = new MutableExecutionContext { TenantId = null };
        innerRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantNote { Id = 1, TenantId = "x", Text = "direct" });

        var decorator = new CachedRepositoryDecorator<TenantNote, int>(innerRepo.Object, cacheService, executionContext: tenantContext);

        await decorator.GetByIdAsync(1);
        await decorator.GetByIdAsync(1);

        innerRepo.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddAsync_InvalidatesExactListKey_ForDistributedCacheProviders()
    {
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheService = new DistributedCacheService(distributedCache, NullLogger<DistributedCacheService>.Instance);

        var items = new List<GlobalNote> { new() { Id = 1, Text = "First" } };
        var innerRepo = new Mock<IRepository<GlobalNote, int>>();
        innerRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => items.ToList());
        innerRepo.Setup(r => r.AddAsync(It.IsAny<GlobalNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GlobalNote n, CancellationToken _) =>
            {
                items.Add(n);
                return n;
            });

        var decorator = new CachedRepositoryDecorator<GlobalNote, int>(innerRepo.Object, cacheService);

        var before = await decorator.GetAllAsync();
        before.Should().HaveCount(1);

        await decorator.AddAsync(new GlobalNote { Id = 2, Text = "Second" });

        var after = await decorator.GetAllAsync();
        after.Should().HaveCount(2);
    }

    public sealed class TenantNote : BaseEntity, ITenantEntity
    {
        public string TenantId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public sealed class GlobalNote : BaseEntity
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class MutableExecutionContext : IExecutionContext
    {
        public string? UserId => null;
        public string? UserName => null;
        public string? TenantId { get; set; }
        public string? CorrelationId => null;
        public string? ClientIpAddress => null;
        public string? UserAgent => null;
        public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
    }
}
