global using Xunit;
global using FluentAssertions;
global using Testcontainers.MsSql;
global using Testcontainers.PostgreSql;
global using Testcontainers.Redis;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
