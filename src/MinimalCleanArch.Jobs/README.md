# MinimalCleanArch.Jobs

Scheduling port (recurring + delayed) with an `IHostedService` fallback. No ASP.NET types on the port.

## Version
- Current: 0.1.20-preview (net9.0, net10.0).

## Why Use It
- register delayed and recurring work behind `IJobScheduler`
- swap the in-process timer for a Wolverine adapter (`AddWolverineJobs`) without changing application handlers

## When to Use It
- generated `--jobs` or `--messaging` apps (sample: purge soft-deleted Todos)
- any host that needs a small scheduler without Hangfire/Quartz

## Dependency Direction
- Depends on: no other MCA package
- Typically referenced by: application (handlers) and composition roots
- Do not reference from: domain projects
- Wolverine adapter lives in `MinimalCleanArch.Messaging` (`AddWolverineJobs`)

## Usage

```bash
dotnet add package MinimalCleanArch.Jobs --version 0.1.20-preview
```

```csharp
builder.Services.AddJobs(options =>
{
    options.Recurring("purge-soft-deleted-todos", TimeSpan.FromHours(24),
        () => new PurgeSoftDeletedTodos(TimeSpan.FromDays(30)));
});
builder.Services.AddScoped<IJobHandler<PurgeSoftDeletedTodos>, PurgeSoftDeletedTodosHandler>();
```

With `--messaging`, call `AddWolverineJobs()` after `AddMinimalCleanArchMessaging` so delayed jobs use `IMessageBus.ScheduleAsync`.
