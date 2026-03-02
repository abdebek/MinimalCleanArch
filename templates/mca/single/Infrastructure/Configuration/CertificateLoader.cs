#if (UseAuth)
using System.Security.Cryptography.X509Certificates;

namespace MCA.Infrastructure.Configuration;

public static class CertificateLoader
{
    public static X509Certificate2? Load(CertificateSettings settings)
    {
        return settings.Source switch
        {
            CertificateSource.File => LoadFromFile(settings),
            CertificateSource.Store => LoadFromStore(settings),
            CertificateSource.Base64 => LoadFromBase64(settings),
            _ => null
        };
    }

    private static X509Certificate2? LoadFromFile(CertificateSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Path) || !File.Exists(settings.Path))
            return null;

#pragma warning disable SYSLIB0057
        return string.IsNullOrEmpty(settings.Password)
            ? new X509Certificate2(settings.Path)
            : new X509Certificate2(settings.Path, settings.Password);
#pragma warning restore SYSLIB0057
    }

    private static X509Certificate2? LoadFromStore(CertificateSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Thumbprint))
            return null;

        var storeName = string.IsNullOrEmpty(settings.StoreName)
            ? StoreName.My
            : Enum.Parse<StoreName>(settings.StoreName, ignoreCase: true);

        var storeLocation = string.IsNullOrEmpty(settings.StoreLocation)
            ? StoreLocation.CurrentUser
            : Enum.Parse<StoreLocation>(settings.StoreLocation, ignoreCase: true);

        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates.Find(
            X509FindType.FindByThumbprint, settings.Thumbprint, validOnly: false);

        return certs.Count > 0 ? certs[0] : null;
    }

    private static X509Certificate2? LoadFromBase64(CertificateSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Base64Encoded))
            return null;

        var bytes = Convert.FromBase64String(settings.Base64Encoded);

#pragma warning disable SYSLIB0057
        return string.IsNullOrEmpty(settings.Password)
            ? new X509Certificate2(bytes)
            : new X509Certificate2(bytes, settings.Password);
#pragma warning restore SYSLIB0057
    }
}
#endif
