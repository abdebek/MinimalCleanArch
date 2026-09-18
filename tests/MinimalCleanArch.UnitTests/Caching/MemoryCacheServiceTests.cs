using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using MinimalCleanArch.Extensions.Caching;

namespace MinimalCleanArch.UnitTests.Caching;

public class MemoryCacheServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_SecondCall_IsCacheHit()
    {
        var cache = new MemoryCacheService(
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<MemoryCacheService>.Instance);
        var factoryCalls = 0;

        var first = await cache.GetOrCreateAsync(
            "todo-list",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult("payload");
            },
            CacheEntryOptions.Absolute(TimeSpan.FromMinutes(5)));
        var second = await cache.GetOrCreateAsync(
            "todo-list",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult("payload");
            },
            CacheEntryOptions.Absolute(TimeSpan.FromMinutes(5)));

        first.Should().Be("payload");
        second.Should().Be("payload");
        factoryCalls.Should().Be(1);
        (await cache.ExistsAsync("todo-list")).Should().BeTrue();
    }
}
