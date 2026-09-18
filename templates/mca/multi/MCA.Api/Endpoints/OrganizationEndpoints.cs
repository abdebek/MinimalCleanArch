#if (UseAuth && UseMultiTenant)
using MCA.Application.Commands;
using MCA.Application.DTOs;
using MCA.Application.Handlers;
#if (UseMessaging)
using Wolverine;
#endif
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Extensions.Extensions;

namespace MCA.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations")
            .WithTags("Organizations")
            .RequireAuthorization();

        group.MapPost("/", CreateOrganization)
            .WithName("CreateOrganization")
            .WithSummary("Create an organization for the current tenant")
            .WithErrorHandling();

        group.MapGet("/current", GetCurrent)
            .WithName("GetCurrentOrganization")
            .WithSummary("Get the organization for the current tenant")
            .WithErrorHandling();

        group.MapGet("/current/roles", GetRoles)
            .WithName("GetOrganizationRoles")
            .WithSummary("List org-scoped roles (data, not Identity seed constants)")
            .WithErrorHandling();

        group.MapGet("/current/members", GetMembers)
            .WithName("GetOrganizationMembers")
            .WithSummary("List members of the current organization")
            .WithErrorHandling();

        group.MapPost("/current/invitations", CreateInvitation)
            .WithName("CreateOrganizationInvitation")
            .WithSummary("Invite a user by code (optional email)")
            .WithErrorHandling();

        group.MapPost("/join", JoinOrganization)
            .WithName("JoinOrganization")
            .WithSummary("Accept an invitation code and switch tenant")
            .WithErrorHandling();
    }

#if (UseMessaging)
    private static async Task<IResult> CreateOrganization(
        CreateOrganizationRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result<OrganizationResponse>>(
            new CreateOrganizationCommand(request.Name), cancellationToken);
        return result.MatchHttp(
            httpContext,
            value => Results.Created($"/api/organizations/current", value));
    }

    private static async Task<IResult> GetCurrent(
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result<OrganizationResponse>>(
            new GetCurrentOrganizationQuery(), cancellationToken);
        return result.MatchHttp(httpContext, Results.Ok);
    }

    private static async Task<IResult> GetRoles(
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result<OrganizationListResult<OrganizationRoleResponse>>>(
            new GetOrganizationRolesQuery(), cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value.Items));
    }

    private static async Task<IResult> GetMembers(
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result<OrganizationListResult<OrganizationMemberResponse>>>(
            new GetOrganizationMembersQuery(), cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value.Items));
    }

    private static async Task<IResult> CreateInvitation(
        CreateInvitationRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result<OrganizationInvitationResponse>>(
            new CreateOrganizationInvitationCommand(request.Role, request.Email, request.ExpiresInDays),
            cancellationToken);
        return result.MatchHttp(
            httpContext,
            value => Results.Created($"/api/organizations/current/invitations/{value.Id}", value));
    }

    private static async Task<IResult> JoinOrganization(
        JoinOrganizationRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result<JoinOrganizationResponse>>(
            new JoinOrganizationCommand(request.Code), cancellationToken);
        return result.MatchHttp(httpContext, Results.Ok);
    }
#else
    private static async Task<IResult> CreateOrganization(
        CreateOrganizationRequest request,
        HttpContext httpContext,
        OrganizationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateOrganizationCommand(request.Name), cancellationToken);
        return result.MatchHttp(
            httpContext,
            value => Results.Created($"/api/organizations/current", value));
    }

    private static async Task<IResult> GetCurrent(
        HttpContext httpContext,
        OrganizationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCurrentOrganizationQuery(), cancellationToken);
        return result.MatchHttp(httpContext, Results.Ok);
    }

    private static async Task<IResult> GetRoles(
        HttpContext httpContext,
        OrganizationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetOrganizationRolesQuery(), cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value.Items));
    }

    private static async Task<IResult> GetMembers(
        HttpContext httpContext,
        OrganizationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetOrganizationMembersQuery(), cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value.Items));
    }

    private static async Task<IResult> CreateInvitation(
        CreateInvitationRequest request,
        HttpContext httpContext,
        OrganizationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateOrganizationInvitationCommand(request.Role, request.Email, request.ExpiresInDays),
            cancellationToken);
        return result.MatchHttp(
            httpContext,
            value => Results.Created($"/api/organizations/current/invitations/{value.Id}", value));
    }

    private static async Task<IResult> JoinOrganization(
        JoinOrganizationRequest request,
        HttpContext httpContext,
        OrganizationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new JoinOrganizationCommand(request.Code), cancellationToken);
        return result.MatchHttp(httpContext, Results.Ok);
    }
#endif
}
#endif
