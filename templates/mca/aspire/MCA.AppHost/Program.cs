var builder = DistributedApplication.CreateBuilder(args);

#if (UsePostgres)
var database = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("appdb");
#elif (UseSqlServer)
var database = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("appdb");
#endif

#if (UseCaching)
var redis = builder.AddRedis("redis")
    .WithDataVolume();
#endif

#if (MultiProject)
var api = builder.AddDotnetProject("api", "../src/MCA.Api/MCA.Api.csproj")
#else
var api = builder.AddDotnetProject("api", "../MCA.csproj")
#endif
#if (UsePostgres || UseSqlServer)
    .WithReference(database)
    .WaitFor(database)
#endif
#if (UseCaching)
    .WithReference(redis)
    .WaitFor(redis)
#endif
    .WithExternalHttpEndpoints();

builder.Build().Run();
