using System.Reflection;

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
    /// Only used with SQL Server persistence.
    /// Default: "wolverine".
    /// </summary>
    public string SchemaName { get; set; } = "wolverine";

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
}
