using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.Security.Encryption;

namespace MinimalCleanArch.Security.EntityFramework;

/// <summary>
/// Extensions for <see cref="ModelBuilder"/> with enhanced security features
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures encryption for properties marked with the <see cref="EncryptedAttribute"/>
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="encryptionService">The encryption service</param>
    /// <param name="serviceProvider">Optional service provider for logging</param>
    /// <returns>The model builder</returns>
    public static ModelBuilder UseEncryption(
        this ModelBuilder modelBuilder, 
        IEncryptionService encryptionService,
        IServiceProvider? serviceProvider = null)
    {
        if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));
        
        if (encryptionService == null)
            throw new ArgumentNullException(nameof(encryptionService));

        var logger = serviceProvider?.GetService<ILogger<EncryptedConverter>>();
        var loggerNonNull = serviceProvider?.GetService<ILogger<EncryptedConverterNonNull>>();

        // Create converters
        var encryptedConverter = new EncryptedConverter(encryptionService, logger);
        var encryptedConverterNonNull = new EncryptedConverterNonNull(encryptionService, loggerNonNull);

        var configuredProperties = 0;

        // Find all entity types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Find properties with the EncryptedAttribute
            var encryptedProperties = entityType.ClrType.GetProperties()
                .Where(p => p.GetCustomAttribute<EncryptedAttribute>() != null);

            foreach (var property in encryptedProperties)
            {
                var propertyType = property.PropertyType;
                var isNullable = false;

                // Handle nullable reference types and Nullable<T>
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType)!;
                    isNullable = true;
                }
                else if (!propertyType.IsValueType)
                {
                    // Reference types - check for nullable reference type annotations
                    var nullabilityContext = new NullabilityInfoContext();
                    var nullabilityInfo = nullabilityContext.Create(property);
                    isNullable = nullabilityInfo.WriteState == NullabilityState.Nullable;
                }

                // Only support string properties for now
                if (propertyType == typeof(string))
                {
                    var entityBuilder = modelBuilder.Entity(entityType.ClrType);
                    var propertyBuilder = entityBuilder.Property(property.Name);

                    // Use appropriate converter based on nullability
                    if (isNullable)
                    {
                        propertyBuilder.HasConversion(encryptedConverter);
                        logger?.LogDebug("Configured nullable encrypted property: {EntityType}.{PropertyName}", 
                            entityType.ClrType.Name, property.Name);
                    }
                    else
                    {
                        // For non-null properties, we still use the nullable converter but with validation
                        // This avoids the nullability mismatch while still providing proper validation
                        propertyBuilder.HasConversion(encryptedConverter);
                        propertyBuilder.IsRequired(); // Ensure the property is required at the database level
                        loggerNonNull?.LogDebug("Configured non-null encrypted property: {EntityType}.{PropertyName}", 
                            entityType.ClrType.Name, property.Name);
                    }

                    configuredProperties++;
                }
                else
                {
                    var generalLogger = serviceProvider?.GetService<ILogger>();
                    generalLogger?.LogWarning("Unsupported property type for encryption: {EntityType}.{PropertyName} ({PropertyType})", 
                        entityType.ClrType.Name, property.Name, propertyType.Name);
                }
            }
        }

        var mainLogger = serviceProvider?.GetService<ILogger>();
        mainLogger?.LogInformation("Configured encryption for {PropertyCount} properties", configuredProperties);

        return modelBuilder;
    }

    /// <summary>
    /// Configures encryption for properties marked with the <see cref="EncryptedAttribute"/> using default converters
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="encryptionService">The encryption service</param>
    /// <returns>The model builder</returns>
    public static ModelBuilder UseEncryption(
        this ModelBuilder modelBuilder, 
        IEncryptionService encryptionService)
    {
        return UseEncryption(modelBuilder, encryptionService, null);
    }

    /// <summary>
    /// Configures encryption for a specific property
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="propertyExpression">Expression to select the property</param>
    /// <param name="encryptionService">The encryption service</param>
    /// <param name="allowNull">Whether the property allows null values</param>
    /// <param name="serviceProvider">Optional service provider for logging</param>
    /// <returns>The model builder</returns>
    public static ModelBuilder UseEncryptionForProperty<TEntity>(
        this ModelBuilder modelBuilder,
        System.Linq.Expressions.Expression<Func<TEntity, string?>> propertyExpression,
        IEncryptionService encryptionService,
        bool allowNull = true,
        IServiceProvider? serviceProvider = null)
        where TEntity : class
    {
        if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));
        
        if (propertyExpression == null)
            throw new ArgumentNullException(nameof(propertyExpression));
        
        if (encryptionService == null)
            throw new ArgumentNullException(nameof(encryptionService));

        var entityBuilder = modelBuilder.Entity<TEntity>();
        var propertyBuilder = entityBuilder.Property(propertyExpression);

        if (allowNull)
        {
            var logger = serviceProvider?.GetService<ILogger<EncryptedConverter>>();
            var converter = new EncryptedConverter(encryptionService, logger);
            propertyBuilder.HasConversion(converter);
        }
        else
        {
            // For non-null properties, use the nullable converter but mark as required
            var logger = serviceProvider?.GetService<ILogger<EncryptedConverter>>();
            var converter = new EncryptedConverter(encryptionService, logger);
            propertyBuilder.HasConversion(converter);
            propertyBuilder.IsRequired();
        }

        return modelBuilder;
    }
}