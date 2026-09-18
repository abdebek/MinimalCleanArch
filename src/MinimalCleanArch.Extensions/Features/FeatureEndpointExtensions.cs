using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Execution;
using MinimalCleanArch.Features;

namespace MinimalCleanArch.Extensions.Features;

public static class FeatureEndpointExtensions
{
    /// <summary>
    /// Returns 403 when <see cref="IFeatureGate"/> disables <paramref name="feature"/>.
    /// Domain handlers stay unaware of the flag.
    /// </summary>
    public static TBuilder RequireFeature<TBuilder>(this TBuilder builder, string feature)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);
        builder.AddEndpointFilter(new FeatureGateFilter(feature));
        return builder;
    }
}

internal sealed class FeatureGateFilter(string feature) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var gate = context.HttpContext.RequestServices.GetRequiredService<IFeatureGate>();
        var tenantId = context.HttpContext.RequestServices.GetService<IExecutionContext>()?.TenantId;
        if (!gate.IsEnabled(feature, tenantId))
        {
            return Results.Problem(
                title: "Feature disabled",
                detail: $"Feature '{feature}' is not enabled.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}
