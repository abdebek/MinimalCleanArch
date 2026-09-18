using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MinimalCleanArch.Extensions.Caching;

namespace MinimalCleanArch.UnitTests.Caching;

public class DistributedCacheServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_SecondCall_IsCacheHit_AndRoundTripsDto()
    {
        var cache = CreateCache();
        var factoryCalls = 0;
        var dto = new CachedTodoDto(42, "cached");

        var first = await cache.GetOrCreateAsync(
            "todo_42",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(dto);
            },
            CacheEntryOptions.Absolute(TimeSpan.FromMinutes(5)));
        var second = await cache.GetOrCreateAsync(
            "todo_42",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(new CachedTodoDto(0, "miss"));
            },
            CacheEntryOptions.Absolute(TimeSpan.FromMinutes(5)));

        first.Should().BeEquivalentTo(dto);
        second.Should().BeEquivalentTo(dto);
        factoryCalls.Should().Be(1);
        (await cache.ExistsAsync("todo_42")).Should().BeTrue();
    }

    private static DistributedCacheService CreateCache()
    {
        IDistributedCache inner = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));
        return new DistributedCacheService(inner, NullLogger<DistributedCacheService>.Instance);
    }

    private sealed record CachedTodoDto(int Id, string Title);
}
