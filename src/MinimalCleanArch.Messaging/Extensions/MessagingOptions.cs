using System.Reflection;
using Wolverine.ErrorHandling;
using Wolverine;

namespace MinimalCleanArch.Messaging.Extensions;

/// <summary>
/// Configuration options for MinimalCleanArch messaging with Wolverine.
/// </summary>
public class MessagingOptions
{
    /// <summary>
    /// Gets or sets the application/service name used for messaging.
    /// Default: Entry assembly name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the database schema name for message persistence tables.
    /// Used with SQL Server and PostgreSQL persistence.
    /// Default: "wolverine".
    /// </summary>
    public string SchemaName { get; set; } = "wolverine";

    /// <summary>
    /// Gets or sets the base local queue name used for in-process domain event handling.
    /// Default: "domain-events".
    /// </summary>
    public string LocalQueueName { get; set; } = "domain-events";

    /// <summary>
    /// Gets or sets an optional prefix applied to generated local queue names.
    /// Default: no prefix.
    /// </summary>
    public string? QueuePrefix { get; set; }

    /// <summary>
    /// Gets or sets the number of parallel listeners for the local queue.
    /// Default: Environment.ProcessorCount.
    /// </summary>
    public int LocalQueueParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets the interval for polling durably stored messages.
    /// Default: 5 seconds.
    /// </summary>
    public TimeSpan DurabilityPollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets a value indicating whether persisted dead-letter messages expire automatically.
    /// Default: false.
    /// </summary>
    public bool DeadLetterQueueExpirationEnabled { get; set; }

    /// <summary>
    /// Gets or sets how long persisted dead-letter messages are retained when expiration is enabled.
    /// Default: Wolverine default.
    /// </summary>
    public TimeSpan? DeadLetterQueueExpiration { get; set; }

    /// <summary>
    /// Gets or sets an optional hook for configuring message failure policies.
    /// </summary>
    public Action<IWithFailurePolicies>? ConfigureFailurePolicies { get; set; }

    /// <summary>
    /// Gets or sets an optional hook for configuring Wolverine policies without dropping to the full options callback.
    /// </summary>
    public Action<IPolicies>? ConfigurePolicies { get; set; }

    /// <summary>
    /// Gets or sets assemblies to scan for message handlers.
    /// If empty, scans the entry assembly.
    /// </summary>
    public List<Assembly> HandlerAssemblies { get; } = new();

    /// <summary>
    /// Adds an assembly to scan for message handlers.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>This options instance for chaining.</returns>
    public MessagingOptions IncludeAssembly(Assembly assembly)
    {
        HandlerAssemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Adds the assembly containing the specified type to scan for message handlers.
    /// </summary>
    /// <typeparam name="T">A type from the assembly to scan.</typeparam>
    /// <returns>This options instance for chaining.</returns>
    public MessagingOptions IncludeAssemblyContaining<T>()
    {
        HandlerAssemblies.Add(typeof(T).Assembly);
        return this;
    }

    /// <summary>
    /// Gets the effective local queue name after applying the optional prefix.
    /// </summary>
    /// <returns>The effective queue name.</returns>
    public string GetEffectiveLocalQueueName()
    {
        return string.IsNullOrWhiteSpace(QueuePrefix)
            ? LocalQueueName
            : $"{QueuePrefix}{LocalQueueName}";
    }
}
