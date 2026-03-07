using MinimalCleanArch.Execution;

namespace MinimalCleanArch.Audit.Services;

internal sealed class ExecutionContextAuditContextProvider : IAuditContextProvider
{
    private readonly IExecutionContext _executionContext;

    public ExecutionContextAuditContextProvider(IExecutionContext executionContext)
    {
        _executionContext = executionContext;
    }

    public string? GetUserId() => _executionContext.UserId;

    public string? GetUserName() => _executionContext.UserName;

    public string? GetTenantId() => _executionContext.TenantId;

    public string? GetCorrelationId() => _executionContext.CorrelationId;

    public string? GetClientIpAddress() => _executionContext.ClientIpAddress;

    public string? GetUserAgent() => _executionContext.UserAgent;

    public IDictionary<string, object>? GetMetadata()
    {
        if (_executionContext.Metadata.Count == 0)
        {
            return null;
        }

        return _executionContext.Metadata.ToDictionary(
            kvp => kvp.Key,
            kvp => (object)kvp.Value);
    }
}
