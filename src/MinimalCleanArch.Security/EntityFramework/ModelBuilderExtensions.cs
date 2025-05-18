using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Security.Encryption;

namespace MinimalCleanArch.Security.EntityFramework;

/// <summary>
/// Extensions for <see cref="ModelBuilder"/>
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures encryption for properties marked with the <see cref="EncryptedAttribute"/>
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="encryptionService">The encryption service</param>
    /// <returns>The model builder</returns>
    public static ModelBuilder UseEncryption(
        this ModelBuilder modelBuilder, 
        IEncryptionService encryptionService)
    {
        if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));
        
        if (encryptionService == null)
            throw new ArgumentNullException(nameof(encryptionService));

        var encryptedConverter = new EncryptedConverter(encryptionService);

        // Find all entity types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Find properties with the EncryptedAttribute
            var encryptedProperties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(string) &&
                           p.GetCustomAttribute<EncryptedAttribute>() != null);

            // Apply the converter to each encrypted property
            foreach (var property in encryptedProperties)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion(encryptedConverter);
            }
        }

        return modelBuilder;
    }
}
