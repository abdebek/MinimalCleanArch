namespace MinimalCleanArch.Messaging.Extensions;

/// <summary>
/// Configuration options for MinimalCleanArch messaging.
/// </summary>
public class MessagingOptions
{
    /// <summary>
    /// Gets or sets the application/service name used for messaging.
    /// Default: Entry assembly name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets whether to enable the transactional outbox pattern.
    /// When enabled, messages are stored in the database before being sent.
    /// Default: true.
    /// </summary>
    public bool EnableOutbox { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically publish domain events on SaveChanges.
    /// Default: true.
    /// </summary>
    public bool AutoPublishDomainEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use the local queue for domain events.
    /// When true, events are processed in-memory. When false, uses the configured transport.
    /// Default: true.
    /// </summary>
    public bool UseLocalQueueForDomainEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of parallel listeners for the local queue.
    /// Default: Environment.ProcessorCount.
    /// </summary>
    public int LocalQueueParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets whether to enable message durability (persist to database).
    /// Default: true when outbox is enabled.
    /// </summary>
    public bool EnableDurability { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval for polling durably stored messages.
    /// Default: 5 seconds.
    /// </summary>
    public TimeSpan DurabilityPollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets assemblies to scan for message handlers.
    /// If empty, scans the entry assembly.
    /// </summary>
    public List<System.Reflection.Assembly> HandlerAssemblies { get; set; } = new();

    /// <summary>
    /// Gets or sets the database schema name for message persistence tables.
    /// Default: "wolverine".
    /// </summary>
    public string SchemaName { get; set; } = "wolverine";
}
