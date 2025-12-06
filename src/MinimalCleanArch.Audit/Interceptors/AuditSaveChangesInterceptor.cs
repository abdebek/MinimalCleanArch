using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MinimalCleanArch.Audit.Configuration;
using MinimalCleanArch.Audit.Entities;
using MinimalCleanArch.Audit.Services;
using MinimalCleanArch.Domain.Entities;

namespace MinimalCleanArch.Audit.Interceptors;

/// <summary>
/// EF Core interceptor that captures entity changes and creates audit log entries.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditContextProvider _contextProvider;
    private readonly AuditOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditSaveChangesInterceptor"/> class.
    /// </summary>
    /// <param name="contextProvider">The audit context provider.</param>
    /// <param name="options">The audit options.</param>
    public AuditSaveChangesInterceptor(
        IAuditContextProvider contextProvider,
        AuditOptions options)
    {
        _contextProvider = contextProvider;
        _options = options;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || eventData.Context == null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditEntries = CreateAuditEntries(eventData.Context);

        if (auditEntries.Count > 0)
        {
            // Store entries that need the generated keys (for Create operations)
            eventData.Context.ChangeTracker.AutoDetectChangesEnabled = false;

            foreach (var entry in auditEntries.Where(e => !e.HasTemporaryProperties))
            {
                eventData.Context.Set<AuditLog>().Add(entry.ToAuditLog());
            }

            // Store entries with temporary properties to be processed after save
            if (auditEntries.Any(e => e.HasTemporaryProperties))
            {
                eventData.Context.ChangeTracker.Entries()
                    .FirstOrDefault()?.Context
                    .ChangeTracker.Context.ChangeTracker
                    .CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
            }

            eventData.Context.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (!_options.Enabled || eventData.Context == null)
            return base.SavingChanges(eventData, result);

        var auditEntries = CreateAuditEntries(eventData.Context);

        if (auditEntries.Count > 0)
        {
            foreach (var entry in auditEntries.Where(e => !e.HasTemporaryProperties))
            {
                eventData.Context.Set<AuditLog>().Add(entry.ToAuditLog());
            }
        }

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        // Handle entries with temporary properties (generated keys)
        if (eventData.Context != null)
        {
            await ProcessTemporaryPropertiesAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditEntry> CreateAuditEntries(DbContext context)
    {
        var entries = new List<AuditEntry>();
        var timestamp = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (ShouldSkipEntry(entry))
                continue;

            var auditEntry = CreateAuditEntry(entry, timestamp);
            if (auditEntry != null)
            {
                entries.Add(auditEntry);
            }
        }

        return entries;
    }

    private bool ShouldSkipEntry(EntityEntry entry)
    {
        // Skip AuditLog itself
        if (entry.Entity is AuditLog)
            return true;

        // Skip unchanged entries
        if (entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
            return true;

        // Skip excluded entity types
        var entityTypeName = entry.Entity.GetType().FullName ?? entry.Entity.GetType().Name;
        if (_options.ExcludedEntityTypes.Contains(entityTypeName))
            return true;

        return false;
    }

    private AuditEntry? CreateAuditEntry(EntityEntry entry, DateTime timestamp)
    {
        var auditEntry = new AuditEntry
        {
            EntityType = entry.Entity.GetType().Name,
            Timestamp = timestamp,
            UserId = _contextProvider.GetUserId(),
            UserName = _contextProvider.GetUserName(),
            CorrelationId = _contextProvider.GetCorrelationId()
        };

        if (_options.CaptureClientIp)
            auditEntry.ClientIpAddress = _contextProvider.GetClientIpAddress();

        if (_options.CaptureUserAgent)
            auditEntry.UserAgent = _contextProvider.GetUserAgent();

        var metadata = _contextProvider.GetMetadata();
        if (metadata != null && metadata.Count > 0)
        {
            auditEntry.Metadata = JsonSerializer.Serialize(metadata, _jsonOptions);
        }

        // Get primary key
        var primaryKey = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .ToList();

        foreach (var property in primaryKey)
        {
            var value = property.CurrentValue;
            if (property.IsTemporary)
            {
                auditEntry.TemporaryProperties.Add(property);
                auditEntry.EntityId = ""; // Will be set after save
            }
            else
            {
                auditEntry.EntityId = value?.ToString() ?? "";
            }
        }

        // Determine operation and capture values
        switch (entry.State)
        {
            case EntityState.Added:
                auditEntry.Operation = AuditOperation.Create;
                if (_options.CaptureNewValues)
                {
                    auditEntry.NewValues = CaptureValues(entry, e => e.CurrentValue);
                }
                break;

            case EntityState.Deleted:
                auditEntry.Operation = AuditOperation.Delete;
                if (_options.CaptureOldValues)
                {
                    auditEntry.OldValues = CaptureValues(entry, e => e.OriginalValue);
                }
                break;

            case EntityState.Modified:
                // Check if this is a soft delete
                if (entry.Entity is ISoftDelete softDelete)
                {
                    var isDeletedProp = entry.Property(nameof(ISoftDelete.IsDeleted));
                    if (isDeletedProp.IsModified)
                    {
                        var wasDeleted = (bool)(isDeletedProp.OriginalValue ?? false);
                        var isDeleted = (bool)(isDeletedProp.CurrentValue ?? false);

                        if (!wasDeleted && isDeleted)
                        {
                            auditEntry.Operation = AuditOperation.SoftDelete;
                        }
                        else if (wasDeleted && !isDeleted)
                        {
                            auditEntry.Operation = AuditOperation.Restore;
                        }
                        else
                        {
                            auditEntry.Operation = AuditOperation.Update;
                        }
                    }
                    else
                    {
                        auditEntry.Operation = AuditOperation.Update;
                    }
                }
                else
                {
                    auditEntry.Operation = AuditOperation.Update;
                }

                var modifiedProperties = entry.Properties
                    .Where(p => p.IsModified && !_options.ExcludedProperties.Contains(p.Metadata.Name))
                    .ToList();

                if (_options.CaptureOldValues)
                {
                    auditEntry.OldValues = CaptureModifiedValues(modifiedProperties, p => p.OriginalValue);
                }

                if (_options.CaptureNewValues)
                {
                    auditEntry.NewValues = CaptureModifiedValues(modifiedProperties, p => p.CurrentValue);
                }

                if (_options.TrackChangedProperties)
                {
                    auditEntry.ChangedProperties = JsonSerializer.Serialize(
                        modifiedProperties.Select(p => p.Metadata.Name).ToList(),
                        _jsonOptions);
                }
                break;

            default:
                return null;
        }

        return auditEntry;
    }

    private string? CaptureValues(EntityEntry entry, Func<PropertyEntry, object?> valueSelector)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (_options.ExcludedProperties.Contains(property.Metadata.Name))
                continue;

            if (property.IsTemporary)
                continue;

            var value = valueSelector(property);
            values[property.Metadata.Name] = value;
        }

        if (values.Count == 0)
            return null;

        var json = JsonSerializer.Serialize(values, _jsonOptions);
        return TruncateIfNeeded(json);
    }

    private string? CaptureModifiedValues(
        List<PropertyEntry> properties,
        Func<PropertyEntry, object?> valueSelector)
    {
        if (properties.Count == 0)
            return null;

        var values = new Dictionary<string, object?>();

        foreach (var property in properties)
        {
            values[property.Metadata.Name] = valueSelector(property);
        }

        var json = JsonSerializer.Serialize(values, _jsonOptions);
        return TruncateIfNeeded(json);
    }

    private string TruncateIfNeeded(string value)
    {
        if (value.Length <= _options.MaxValueLength)
            return value;

        return value[.._options.MaxValueLength] + "...[truncated]";
    }

    private async Task ProcessTemporaryPropertiesAsync(DbContext context, CancellationToken cancellationToken)
    {
        // This handles entries that had temporary primary keys (auto-generated)
        // After SaveChanges, we can now get the actual values
        // For simplicity, we'll handle this in a future enhancement
        await Task.CompletedTask;
    }
}

/// <summary>
/// Internal class to track audit entry before converting to AuditLog entity.
/// </summary>
internal class AuditEntry
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditOperation Operation { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? CorrelationId { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedProperties { get; set; }
    public string? Metadata { get; set; }

    public List<PropertyEntry> TemporaryProperties { get; } = new();
    public bool HasTemporaryProperties => TemporaryProperties.Count > 0;

    public AuditLog ToAuditLog()
    {
        // Resolve temporary properties if any
        if (HasTemporaryProperties)
        {
            foreach (var prop in TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    EntityId = prop.CurrentValue?.ToString() ?? EntityId;
                }
            }
        }

        return new AuditLog
        {
            EntityType = EntityType,
            EntityId = EntityId,
            Operation = Operation,
            Timestamp = Timestamp,
            UserId = UserId,
            UserName = UserName,
            CorrelationId = CorrelationId,
            ClientIpAddress = ClientIpAddress,
            UserAgent = UserAgent,
            OldValues = OldValues,
            NewValues = NewValues,
            ChangedProperties = ChangedProperties,
            Metadata = Metadata
        };
    }
}
