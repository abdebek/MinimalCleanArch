using System.Security.Claims;

namespace MinimalCleanArch.Execution;

/// <summary>
/// Configures claim resolution for built-in execution-context implementations.
/// </summary>
public sealed class ExecutionContextOptions
{
    /// <summary>
    /// Gets the claim types used to resolve the current user identifier.
    /// </summary>
    public IList<string> UserIdClaimTypes { get; } =
    [
        ClaimTypes.NameIdentifier,
        "sub"
    ];

    /// <summary>
    /// Gets the claim types used to resolve the current user name before <see cref="System.Security.Principal.IIdentity.Name"/>.
    /// </summary>
    public IList<string> UserNameClaimTypes { get; } =
    [
        ClaimTypes.Name,
        "name",
        "preferred_username"
    ];

    /// <summary>
    /// Gets the claim types used as a final fallback for the current user name.
    /// </summary>
    public IList<string> UserNameFallbackClaimTypes { get; } =
    [
        ClaimTypes.Email,
        "email"
    ];

    /// <summary>
    /// Gets the claim types used to resolve the current tenant identifier.
    /// </summary>
    public IList<string> TenantIdClaimTypes { get; } =
    [
        "tenant_id"
    ];
}
