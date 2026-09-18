#if (UseAuth && UseMultiTenant)
using MCA.Application.Commands;
using MCA.Application.DTOs;
using MCA.Application.Identity;
using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Execution;
using MinimalCleanArch.Repositories;

namespace MCA.Application.Handlers;

public class OrganizationCommandHandler
{
    public const string OwnerRole = "Owner";
    public const string AdminRole = "Admin";
    public const string MemberRole = "Member";

    private static readonly string[] DefaultRoles = [OwnerRole, AdminRole, MemberRole];

    private readonly IOrganizationRepository _organizations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExecutionContext _executionContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrganizationCommandHandler(
        IOrganizationRepository organizations,
        IUnitOfWork unitOfWork,
        IExecutionContext executionContext,
        UserManager<ApplicationUser> userManager)
    {
        _organizations = organizations;
        _unitOfWork = unitOfWork;
        _executionContext = executionContext;
        _userManager = userManager;
    }

    public async Task<Result<OrganizationResponse>> Handle(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenant(out var tenantId) || !TryGetUserId(out var userId))
        {
            return Result.Failure<OrganizationResponse>(
                Error.Unauthorized("ORG.UNAUTHENTICATED", "Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<OrganizationResponse>(
                Error.Validation("ORG.NAME_REQUIRED", "Organization name is required."));
        }

        if (!Guid.TryParse(tenantId, out var organizationId))
        {
            return Result.Failure<OrganizationResponse>(
                Error.Validation("ORG.INVALID_TENANT", "Current tenant id is not a valid organization key."));
        }

        if (await _organizations.GetCurrentAsync(cancellationToken) is not null)
        {
            return Result.Failure<OrganizationResponse>(
                Error.Conflict("ORG.EXISTS", "An organization already exists for this tenant."));
        }

        var organization = new Organization(organizationId, command.Name, tenantId);
        await _organizations.AddAsync(organization, cancellationToken);
        foreach (var roleName in DefaultRoles)
        {
            await _organizations.AddRoleAsync(new OrganizationRole(roleName, tenantId), cancellationToken);
        }

        await _organizations.AddMembershipAsync(
            new OrganizationMembership(organizationId, userId, OwnerRole, tenantId),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new OrganizationResponse(organization.Id, organization.Name, tenantId, OwnerRole));
    }

    public async Task<Result<OrganizationResponse>> Handle(
        GetCurrentOrganizationQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<OrganizationResponse>(
                Error.Unauthorized("ORG.UNAUTHENTICATED", "Authentication is required."));
        }

        var organization = await _organizations.GetCurrentAsync(cancellationToken);
        if (organization is null)
        {
            return Result.Failure<OrganizationResponse>(
                Error.NotFound("ORG.NOT_FOUND", "No organization for the current tenant."));
        }

        var membership = await _organizations.GetMembershipAsync(userId, cancellationToken);
        return Result.Success(new OrganizationResponse(
            organization.Id,
            organization.Name,
            organization.TenantId,
            membership?.Role ?? string.Empty));
    }

    public async Task<Result<OrganizationListResult<OrganizationRoleResponse>>> Handle(
        GetOrganizationRolesQuery query,
        CancellationToken cancellationToken)
    {
        if (await _organizations.GetCurrentAsync(cancellationToken) is null)
        {
            return Result.Failure<OrganizationListResult<OrganizationRoleResponse>>(
                Error.NotFound("ORG.NOT_FOUND", "No organization for the current tenant."));
        }

        var roles = await _organizations.GetRolesAsync(cancellationToken);
        var items = roles.Select(r => new OrganizationRoleResponse(r.Id, r.Name)).ToList();
        return Result.Success(new OrganizationListResult<OrganizationRoleResponse>(items));
    }

    public async Task<Result<OrganizationListResult<OrganizationMemberResponse>>> Handle(
        GetOrganizationMembersQuery query,
        CancellationToken cancellationToken)
    {
        if (await _organizations.GetCurrentAsync(cancellationToken) is null)
        {
            return Result.Failure<OrganizationListResult<OrganizationMemberResponse>>(
                Error.NotFound("ORG.NOT_FOUND", "No organization for the current tenant."));
        }

        var members = await _organizations.GetMembersAsync(cancellationToken);
        var items = members.Select(m => new OrganizationMemberResponse(m.UserId, m.Role)).ToList();
        return Result.Success(new OrganizationListResult<OrganizationMemberResponse>(items));
    }

    public async Task<Result<OrganizationInvitationResponse>> Handle(
        CreateOrganizationInvitationCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<OrganizationInvitationResponse>(
                Error.Unauthorized("ORG.UNAUTHENTICATED", "Authentication is required."));
        }

        var organization = await _organizations.GetCurrentAsync(cancellationToken);
        if (organization is null)
        {
            return Result.Failure<OrganizationInvitationResponse>(
                Error.NotFound("ORG.NOT_FOUND", "Create an organization before inviting members."));
        }

        var membership = await _organizations.GetMembershipAsync(userId, cancellationToken);
        if (membership is null || (membership.Role != OwnerRole && membership.Role != AdminRole))
        {
            return Result.Failure<OrganizationInvitationResponse>(
                Error.Forbidden("ORG.INVITE_FORBIDDEN", "Only Owner or Admin can invite members."));
        }

        if (string.IsNullOrWhiteSpace(command.Role))
        {
            return Result.Failure<OrganizationInvitationResponse>(
                Error.Validation("ORG.ROLE_REQUIRED", "Invitation role is required."));
        }

        if (!await _organizations.RoleExistsAsync(command.Role.Trim(), cancellationToken))
        {
            return Result.Failure<OrganizationInvitationResponse>(
                Error.Validation("ORG.UNKNOWN_ROLE", $"Role '{command.Role}' is not defined for this organization."));
        }

        var invitation = OrganizationInvitation.Create(
            organization.Id,
            organization.TenantId,
            command.Role,
            userId,
            command.Email,
            command.ExpiresInDays <= 0 ? 7 : command.ExpiresInDays);
        await _organizations.AddInvitationAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new OrganizationInvitationResponse(
            invitation.Id,
            invitation.Code,
            invitation.Role,
            invitation.Email,
            invitation.ExpiresAt));
    }

    public async Task<Result<JoinOrganizationResponse>> Handle(
        JoinOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<JoinOrganizationResponse>(
                Error.Unauthorized("ORG.UNAUTHENTICATED", "Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return Result.Failure<JoinOrganizationResponse>(
                Error.Validation("ORG.CODE_REQUIRED", "Invitation code is required."));
        }

        var invitation = await _organizations.GetInvitationByCodeAsync(command.Code.Trim(), cancellationToken);
        if (invitation is null || !invitation.IsPending)
        {
            return Result.Failure<JoinOrganizationResponse>(
                Error.NotFound("ORG.INVITE_INVALID", "Invitation is missing, expired, or already accepted."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<JoinOrganizationResponse>(
                Error.NotFound("ORG.USER_NOT_FOUND", "Current user was not found."));
        }

        if (!string.IsNullOrEmpty(invitation.Email)
            && !string.Equals(invitation.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<JoinOrganizationResponse>(
                Error.Forbidden("ORG.INVITE_EMAIL", "This invitation was issued for a different email."));
        }

        var organization = await _organizations.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return Result.Failure<JoinOrganizationResponse>(
                Error.NotFound("ORG.NOT_FOUND", "Organization for this invitation was not found."));
        }

        user.TenantId = organization.TenantId;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            var errors = string.Join("; ", update.Errors.Select(e => e.Description));
            return Result.Failure<JoinOrganizationResponse>(
                Error.Failure("ORG.JOIN_FAILED", errors));
        }

        await _organizations.AddMembershipAsync(
            new OrganizationMembership(organization.Id, userId, invitation.Role, organization.TenantId),
            cancellationToken);
        invitation.Accept(userId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new JoinOrganizationResponse(organization.Id, organization.TenantId, invitation.Role));
    }

    private bool TryGetTenant(out string tenantId)
    {
        tenantId = _executionContext.TenantId ?? string.Empty;
        return !string.IsNullOrWhiteSpace(tenantId);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(_executionContext.UserId, out userId);
}
#endif
