var builder = DistributedApplication.CreateBuilder(args);

// Postgres resource; connection name "mca" is injected into the API as ConnectionStrings__mca
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("mca-aspire-postgres")
    .AddDatabase("mca");

// Redis for distributed cache demos; connection name "redis"
var redis = builder.AddRedis("redis")
    .WithDataVolume("mca-aspire-redis");

builder.AddDotnetProject("api", "../../MinimalCleanArch.Sample/MinimalCleanArch.Sample.csproj")
    .WithReference(postgres)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

builder.Build().Run();
