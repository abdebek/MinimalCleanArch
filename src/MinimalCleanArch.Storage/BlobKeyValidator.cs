namespace MinimalCleanArch.Storage;

/// <summary>
/// Normalizes and guards blob keys against path traversal and absolute paths.
/// Keys are expected to be relative paths within the container/bucket (e.g. <c>uploads/report.pdf</c>).
/// </summary>
public static class BlobKeyValidator
{
    /// <summary>
    /// Normalizes a blob key: trims, replaces backslashes with forward slashes, collapses
    /// <c>./</c> segments, and rejects <c>..</c> traversal or absolute/UNC paths.
    /// Returns the normalized key, or <c>null</c> when the key is invalid.
    /// </summary>
    public static string? TryNormalize(string blobKey)
    {
        if (string.IsNullOrWhiteSpace(blobKey))
        {
            return null;
        }

        var key = blobKey.Trim().Replace('\\', '/');

        if (key.Length == 0)
        {
            return null;
        }

        // Reject absolute/UNC paths and protocol-relative URLs.
        if (key.StartsWith('/')
            || key.StartsWith("//", StringComparison.Ordinal)
            || key.Contains(':', StringComparison.Ordinal))
        {
            return null;
        }

        // Reject path traversal segments. Split on '/' so "..foo" is allowed but "../x" and "x/.." are not.
        var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var normalized = new List<string>(segments.Length);
        foreach (var segment in segments)
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                return null;
            }

            normalized.Add(segment);
        }

        return string.Join('/', normalized);
    }

    /// <summary>
    /// Normalizes a blob key or throws <see cref="ArgumentException"/> when it is invalid.
    /// </summary>
    public static string NormalizeOrThrow(string blobKey)
    {
        return TryNormalize(blobKey)
            ?? throw new ArgumentException(
                "Blob key must be a relative path without '..' traversal, absolute paths, or drive specifiers.",
                nameof(blobKey));
    }
}