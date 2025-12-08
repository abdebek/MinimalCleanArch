using MinimalCleanArch.Domain.Common;

namespace MCA.Domain;

/// <summary>
/// Application-specific error definitions.
/// </summary>
public static class DomainErrors
{
    public static class General
    {
        public static Error NotFound(string resource, object id) =>
            Error.NotFound($"{resource}.NotFound", $"{resource} with id '{id}' was not found.");
    }
}
