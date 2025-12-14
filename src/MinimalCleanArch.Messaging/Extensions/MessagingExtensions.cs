using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.SqlServer;
using Wolverine.Postgresql;

namespace MinimalCleanArch.Messaging.Extensions;

/// <summary>
/// Extension methods for configuring MinimalCleanArch messaging with Wolverine.
/// </summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Adds MinimalCleanArch messaging services using Wolverine (in-memory mode).
    /// Use this for development or when durable messaging is not required.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional action to configure messaging options.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddMinimalCleanArchMessaging(
        this IHostApplicationBuilder builder,
        Action<MessagingOptions>? configure = null)
    {
        var options = new MessagingOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton(options);

        var serviceName = options.ServiceName
            ?? Assembly.GetEntryAssembly()?.GetName().Name
            ?? "MinimalCleanArch";

        builder.UseWolverine(opts =>
        {
            ConfigureWolverineBase(opts, options, serviceName);

            // In-memory mode - no persistence
            opts.Durability.Mode = DurabilityMode.Solo;
        });

        // Register domain event services
        builder.Services.AddDomainEventPublishing();

        return builder;
    }

    /// <summary>
    /// Adds MinimalCleanArch messaging services using Wolverine with SQL Server persistence.
    /// Enables the transactional outbox pattern for reliable messaging.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionString">SQL Server connection string.</param>
    /// <param name="configure">Optional action to configure messaging options.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddMinimalCleanArchMessagingWithSqlServer(
        this IHostApplicationBuilder builder,
        string connectionString,
        Action<MessagingOptions>? configure = null)
    {
        var options = new MessagingOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton(options);

        var serviceName = options.ServiceName
            ?? Assembly.GetEntryAssembly()?.GetName().Name
            ?? "MinimalCleanArch";

        builder.UseWolverine(opts =>
        {
            ConfigureWolverineBase(opts, options, serviceName);

            // SQL Server persistence for outbox
            opts.PersistMessagesWithSqlServer(connectionString, options.SchemaName);

            // Enable EF Core transaction integration
            opts.UseEntityFrameworkCoreTransactions();

            // Durable mode for SQL Server - enables outbox pattern
            opts.Durability.Mode = DurabilityMode.Balanced;
            opts.Durability.ScheduledJobPollingTime = options.DurabilityPollingInterval;
        });

        // Register domain event services
        builder.Services.AddDomainEventPublishing();

        return builder;
    }

    /// <summary>
    /// Adds MinimalCleanArch messaging services using Wolverine with PostgreSQL persistence.
    /// Enables the transactional outbox pattern for reliable messaging.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="configure">Optional action to configure messaging options.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddMinimalCleanArchMessagingWithPostgres(
        this IHostApplicationBuilder builder,
        string connectionString,
        Action<MessagingOptions>? configure = null)
    {
        var options = new MessagingOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton(options);

        var serviceName = options.ServiceName
            ?? Assembly.GetEntryAssembly()?.GetName().Name
            ?? "MinimalCleanArch";

        builder.UseWolverine(opts =>
        {
            ConfigureWolverineBase(opts, options, serviceName);

            // PostgreSQL persistence for outbox
            opts.PersistMessagesWithPostgresql(connectionString, options.SchemaName);

            // Enable EF Core transaction integration
            opts.UseEntityFrameworkCoreTransactions();

            // Durable mode for PostgreSQL - enables outbox pattern
            opts.Durability.Mode = DurabilityMode.Balanced;
            opts.Durability.ScheduledJobPollingTime = options.DurabilityPollingInterval;
        });

        // Register domain event services
        builder.Services.AddDomainEventPublishing();

        return builder;
    }

    /// <summary>
    /// Adds MinimalCleanArch messaging services using Wolverine with custom configuration.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configureOptions">Action to configure messaging options.</param>
    /// <param name="configureWolverine">Action to configure Wolverine directly.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddMinimalCleanArchMessaging(
        this IHostApplicationBuilder builder,
        Action<MessagingOptions> configureOptions,
        Action<WolverineOptions> configureWolverine)
    {
        var options = new MessagingOptions();
        configureOptions(options);

        builder.Services.AddSingleton(options);

        var serviceName = options.ServiceName
            ?? Assembly.GetEntryAssembly()?.GetName().Name
            ?? "MinimalCleanArch";

        builder.UseWolverine(opts =>
        {
            ConfigureWolverineBase(opts, options, serviceName);

            // Apply custom Wolverine configuration
            configureWolverine(opts);
        });

        // Register domain event services
        builder.Services.AddDomainEventPublishing();

        return builder;
    }

    private static void ConfigureWolverineBase(
        WolverineOptions opts,
        MessagingOptions options,
        string serviceName)
    {
        opts.ServiceName = serviceName;

        // Configure handler discovery
        if (options.HandlerAssemblies.Count > 0)
        {
            foreach (var assembly in options.HandlerAssemblies)
            {
                opts.Discovery.IncludeAssembly(assembly);
            }
        }
        else
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                opts.Discovery.IncludeAssembly(entryAssembly);
            }
        }

        // Configure local queue for domain events with parallelism
        opts.LocalQueue("domain-events")
            .MaximumParallelMessages(options.LocalQueueParallelism);

        // Auto-apply transactions
        opts.Policies.AutoApplyTransactions();
    }
}
