using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Audit.Configuration;
using MinimalCleanArch.Audit.Entities;
using MinimalCleanArch.Audit.Interceptors;
using MinimalCleanArch.Audit.Services;

namespace MinimalCleanArch.Audit.Extensions;

/// <summary>
/// Extension methods for adding audit logging to the application.
/// </summary>
public static class AuditExtensions
{
    /// <summary>
    /// Adds audit logging services to the service collection.
    /// This is an opt-in feature that must be explicitly enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure audit options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuditLogging(
        this IServiceCollection services,
        Action<AuditOptions>? configure = null)
    {
        var options = new AuditOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<IAuditContextProvider, HttpContextAuditContextProvider>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        return services;
    }

    /// <summary>
    /// Adds audit logging services with a custom context provider.
    /// </summary>
    /// <typeparam name="TContextProvider">The custom context provider type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure audit options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuditLogging<TContextProvider>(
        this IServiceCollection services,
        Action<AuditOptions>? configure = null)
        where TContextProvider : class, IAuditContextProvider
    {
        var options = new AuditOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<IAuditContextProvider, TContextProvider>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        return services;
    }

    /// <summary>
    /// Adds the audit log query service.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type that contains the AuditLog DbSet.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuditLogService<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IAuditLogService>(sp =>
            new AuditLogService(sp.GetRequiredService<TContext>()));

        return services;
    }

    /// <summary>
    /// Configures the DbContext options to use the audit interceptor.
    /// Call this when configuring your DbContext.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The options builder for chaining.</returns>
    public static DbContextOptionsBuilder UseAuditInterceptor(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetService<AuditSaveChangesInterceptor>();
        if (interceptor != null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        return optionsBuilder;
    }

    /// <summary>
    /// Configures the model to include the AuditLog entity.
    /// Call this in your DbContext's OnModelCreating method.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="options">Optional audit options for table configuration.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder UseAuditLog(
        this ModelBuilder modelBuilder,
        AuditOptions? options = null)
    {
        options ??= new AuditOptions();

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable(options.TableName, options.Schema);

            entity.HasKey(e => e.Id);

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.EntityId)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.UserId)
                .HasMaxLength(256);

            entity.Property(e => e.UserName)
                .HasMaxLength(256);

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(64);

            entity.Property(e => e.ClientIpAddress)
                .HasMaxLength(45); // IPv6 max length

            entity.Property(e => e.UserAgent)
                .HasMaxLength(512);

            // Indexes for common query patterns
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.Operation);
        });

        return modelBuilder;
    }
}
