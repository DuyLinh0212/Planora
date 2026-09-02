using Microsoft.EntityFrameworkCore;
using Planora.Application.Authorization;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Projects;
using Planora.Domain.Users;
using Planora.Application.Billing;

namespace Planora.Application.ProjectMembers;

public sealed class ProjectMemberService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    IProjectPermissionService projectPermissionService,
    ITokenIssuer tokenIssuer,
    SubscriptionQuotaService subscriptionQuotaService,
    IRealtimeNotifier realtimeNotifier,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<ProjectInvitationResponse>> InviteProjectMemberAsync(Guid projectId, InviteProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await GetProjectMemberAuthorizationErrorAsync(projectId, PermissionCodes.ProjectManageMembers, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<ProjectInvitationResponse>(authorizationError);
        var quotaError = await subscriptionQuotaService.GetProjectMemberQuotaErrorAsync(projectId, cancellationToken);
        if (quotaError is not null)
            return ApplicationResult.Failure<ProjectInvitationResponse>(quotaError);
        if (string.IsNullOrWhiteSpace(request.Email))
            return ApplicationResult.Failure<ProjectInvitationResponse>(ApplicationErrors.Validation("invitation.email_required", "Email is required.", "email"));

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var invitedUser = await dbContext.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        var roleBelongsToProject = await dbContext.ProjectRoles.AnyAsync(role => role.Id == request.RoleId && role.ProjectId == projectId, cancellationToken);
        if (!roleBelongsToProject)
            return ApplicationResult.Failure<ProjectInvitationResponse>(ApplicationErrors.Validation("invitation.invalid_role", "Role does not belong to this project.", "roleId"));

        var invitationToken = tokenIssuer.CreateOpaqueToken();
        var currentTime = timeProvider.GetUtcNow();
        var invitation = ProjectInvitation.CreateProjectInvitation(projectId, request.Email, invitedUser?.Id, currentUser.UserId!.Value, request.RoleId, invitationToken.Hash, currentTime.AddDays(Math.Clamp(request.ExpiresInDays, 1, 30)), currentTime);
        dbContext.ProjectInvitations.Add(invitation);
        if (invitedUser is not null)
            dbContext.UserNotifications.Add(UserNotification.CreateUserNotification(
                invitedUser.Id,
                "project.invitation",
                "Bạn có lời mời dự án mới",
                "Một project leader đã mời bạn tham gia dự án. Mở Planora để chấp nhận hoặc từ chối.",
                nameof(ProjectInvitation),
                invitation.Id.ToString(),
                currentTime));
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUser.UserId, projectId, "invitation.created", nameof(ProjectInvitation), invitation.Id.ToString(), null, null, currentUser.IpAddress, currentTime));
        await dbContext.SaveChangesAsync(cancellationToken);

        if (invitedUser is not null)
        {
            await realtimeNotifier.NotifyUserAsync(
                invitedUser.Id,
                "ProjectInvitationReceived",
                new
                {
                    userId = invitedUser.Id,
                    projectId,
                    invitationId = invitation.Id,
                    email = invitation.InvitedEmail,
                    expiresAt = invitation.ExpiresAt
                },
                cancellationToken);
        }

        return ApplicationResult.Success(new ProjectInvitationResponse(
            invitation.Id,
            invitation.InvitedEmail,
            invitation.Status,
            invitation.ExpiresAt,
            invitation.CreatedAt,
            invitationToken.Value));
    }

    public async Task<ApplicationResult> AcceptProjectInvitationAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var currentUserId = currentUser.UserId;
        if (currentUserId is null)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());

        var user = await dbContext.Users.FindAsync([currentUserId.Value], cancellationToken);
        var invitation = await dbContext.ProjectInvitations.FindAsync([invitationId], cancellationToken);
        if (user is null || invitation is null || !string.Equals(invitation.InvitedEmail.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Invitation"));

        var currentTime = timeProvider.GetUtcNow();
        var acceptanceResult = invitation.AcceptProjectInvitation(currentTime);
        if (!acceptanceResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(acceptanceResult.Code!, acceptanceResult.Message!));

        var userIsAlreadyMember = await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == invitation.ProjectId && member.UserId == currentUserId && member.Status == MembershipStatus.Active, cancellationToken);
        if (!userIsAlreadyMember)
        {
            var newMember = ProjectMember.AddProjectMember(invitation.ProjectId, currentUserId.Value, currentTime);
            dbContext.ProjectMembers.Add(newMember);
            dbContext.ProjectMemberRoles.Add(ProjectMemberRole.CreateProjectMemberRole(newMember.Id, invitation.RoleId));
        }

        await MarkInvitationNotificationsReadAsync(currentUserId.Value, invitation.Id, currentTime, cancellationToken);
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUserId, invitation.ProjectId, "invitation.accepted", nameof(ProjectInvitation), invitation.Id.ToString(), null, null, currentUser.IpAddress, currentTime));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RejectProjectInvitationAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var currentUserId = currentUser.UserId;
        if (currentUserId is null)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());

        var user = await dbContext.Users.FindAsync([currentUserId.Value], cancellationToken);
        var invitation = await dbContext.ProjectInvitations.FindAsync([invitationId], cancellationToken);
        if (user is null || invitation is null || !string.Equals(invitation.InvitedEmail.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Invitation"));

        var currentTime = timeProvider.GetUtcNow();
        var rejectionResult = invitation.RejectProjectInvitation(currentTime);
        if (!rejectionResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(rejectionResult.Code!, rejectionResult.Message!));

        await MarkInvitationNotificationsReadAsync(currentUserId.Value, invitation.Id, currentTime, cancellationToken);
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUserId, invitation.ProjectId, "invitation.rejected", nameof(ProjectInvitation), invitation.Id.ToString(), null, null, currentUser.IpAddress, currentTime));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task MarkInvitationNotificationsReadAsync(Guid userId, Guid invitationId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var entityId = invitationId.ToString();
        var notifications = await dbContext.UserNotifications
            .Where(notification =>
                notification.UserId == userId &&
                notification.Type == "project.invitation" &&
                notification.EntityType == nameof(ProjectInvitation) &&
                notification.EntityId == entityId &&
                notification.ReadAt == null &&
                notification.DeletedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var notification in notifications)
            notification.MarkUserNotificationRead(readAt);
    }

    public async Task<ApplicationResult<IReadOnlyList<ProjectMemberResponse>>> GetProjectMembersAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var authorizationError = await GetProjectMemberAuthorizationErrorAsync(projectId, PermissionCodes.ProjectView, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<ProjectMemberResponse>>(authorizationError);

        var members = await (
            from member in dbContext.ProjectMembers
            join user in dbContext.Users on member.UserId equals user.Id
            where member.ProjectId == projectId
            select new { Member = member, User = user }).ToListAsync(cancellationToken);
        var membershipIds = members.Select(item => item.Member.Id).ToArray();
        var memberRoles = await (
            from memberRole in dbContext.ProjectMemberRoles
            join role in dbContext.ProjectRoles on memberRole.RoleId equals role.Id
            where membershipIds.Contains(memberRole.ProjectMemberId)
            select new { memberRole.ProjectMemberId, role.Name }).ToListAsync(cancellationToken);

        var response = members.Select(item => new ProjectMemberResponse(
            item.Member.Id,
            item.User.Id,
            item.User.DisplayName,
            item.User.Email,
            item.Member.Status,
            memberRoles.Where(role => role.ProjectMemberId == item.Member.Id).Select(role => role.Name).ToArray())).ToArray();
        return ApplicationResult.Success<IReadOnlyList<ProjectMemberResponse>>(response);
    }

    public async Task<ApplicationResult<IReadOnlyList<RegisteredUserMatchResponse>>> FindRegisteredUsersAsync(Guid projectId, string query, CancellationToken cancellationToken)
    {
        var authorizationError = await GetProjectMemberAuthorizationErrorAsync(projectId, PermissionCodes.ProjectManageMembers, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<RegisteredUserMatchResponse>>(authorizationError);
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
            return ApplicationResult.Failure<IReadOnlyList<RegisteredUserMatchResponse>>(ApplicationErrors.Validation("member_lookup.query_too_short", "Enter at least 3 characters of an email, username, or display name.", "query"));
        var normalized = query.Trim().ToUpperInvariant();
        var matches = await dbContext.Users
            .Where(user => user.Status == UserStatus.Active &&
                (user.NormalizedEmail.Contains(normalized) || user.NormalizedUsername.Contains(normalized) || user.DisplayName.ToUpper().Contains(normalized)))
            .OrderBy(user => user.DisplayName)
            .Take(8)
            .Select(user => new RegisteredUserMatchResponse(
                user.Id,
                user.DisplayName,
                user.Email,
                user.AvatarUrl,
                dbContext.ProjectMembers.Any(member => member.ProjectId == projectId && member.UserId == user.Id && member.Status == MembershipStatus.Active)))
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success<IReadOnlyList<RegisteredUserMatchResponse>>(matches);
    }

    public async Task<ApplicationResult<IReadOnlyList<ProjectInvitationSummaryResponse>>> GetProjectInvitationsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var authorizationError = await GetProjectMemberAuthorizationErrorAsync(projectId, PermissionCodes.ProjectManageMembers, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<ProjectInvitationSummaryResponse>>(authorizationError);
        var invitations = await dbContext.ProjectInvitations
            .Where(invitation => invitation.ProjectId == projectId)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => new ProjectInvitationSummaryResponse(invitation.Id, invitation.InvitedEmail, invitation.Status, invitation.ExpiresAt, invitation.CreatedAt))
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success<IReadOnlyList<ProjectInvitationSummaryResponse>>(invitations);
    }

    public async Task<ApplicationResult<IReadOnlyList<ProjectRoleOptionResponse>>> GetProjectRolesAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var authorizationError = await GetProjectMemberAuthorizationErrorAsync(projectId, PermissionCodes.ProjectView, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<ProjectRoleOptionResponse>>(authorizationError);

        var roles = await dbContext.ProjectRoles
            .Where(role => role.ProjectId == projectId && role.Code != DefaultProjectRoleCodes.Owner)
            .OrderBy(role => role.Name)
            .Select(role => new ProjectRoleOptionResponse(role.Id, role.Code, role.Name, role.IsSystemRole))
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success<IReadOnlyList<ProjectRoleOptionResponse>>(roles);
    }

    public async Task<ApplicationResult> ChangeProjectMemberRoleAsync(Guid projectId, Guid membershipId, ChangeProjectMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await GetProjectMemberAuthorizationErrorAsync(projectId, PermissionCodes.ProjectManageMembers, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var member = await dbContext.ProjectMembers.FindAsync([membershipId], cancellationToken);
        var project = await dbContext.Projects.FindAsync([projectId], cancellationToken);
        var role = await dbContext.ProjectRoles.FindAsync([request.RoleId], cancellationToken);
        if (member is null || member.ProjectId != projectId || project is null || role?.ProjectId != projectId)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Member or role"));
        if (role.Code == DefaultProjectRoleCodes.Owner)
            return ApplicationResult.Failure(ApplicationErrors.Conflict("project.owner_role_internal", "Owner is an internal project property and cannot be assigned as a workspace role."));

        var currentRoles = await dbContext.ProjectMemberRoles.Where(memberRole => memberRole.ProjectMemberId == membershipId).ToListAsync(cancellationToken);
        dbContext.ProjectMemberRoles.RemoveRange(currentRoles);
        dbContext.ProjectMemberRoles.Add(ProjectMemberRole.CreateProjectMemberRole(membershipId, role.Id));
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUser.UserId, projectId, "member.role_changed", nameof(ProjectMember), membershipId.ToString(), null, $"{{\"roleId\":\"{role.Id}\"}}", currentUser.IpAddress, timeProvider.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RemoveProjectMemberAsync(Guid projectId, Guid membershipId, RemoveProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await GetProjectMemberAuthorizationErrorAsync(projectId, PermissionCodes.ProjectManageMembers, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (string.IsNullOrWhiteSpace(request.Reason))
            return ApplicationResult.Failure(ApplicationErrors.Validation("member.removal_reason_required", "Explain why this member is being removed.", "reason"));

        var member = await dbContext.ProjectMembers.FindAsync([membershipId], cancellationToken);
        var project = await dbContext.Projects.FindAsync([projectId], cancellationToken);
        if (member is null || member.ProjectId != projectId || project is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Member"));

        var removalResult = member.RemoveProjectMember(member.UserId == project.OwnerUserId);
        if (!removalResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(removalResult.Code!, removalResult.Message!));

        var now = timeProvider.GetUtcNow();
        dbContext.UserNotifications.Add(UserNotification.CreateUserNotification(
            member.UserId,
            "project.member_removed",
            "Bạn đã được rời khỏi dự án",
            $"Lý do: {request.Reason.Trim()}",
            nameof(Project),
            projectId.ToString(),
            now));
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUser.UserId, projectId, "member.removed", nameof(ProjectMember), membershipId.ToString(), null, $"{{\"reason\":\"{request.Reason.Trim().Replace("\"", "\\\"")}\"}}", currentUser.IpAddress, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationError?> GetProjectMemberAuthorizationErrorAsync(Guid projectId, string permissionCode, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();
        return await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, permissionCode, null, cancellationToken)
            ? null
            : ApplicationErrors.NotFound("Project");
    }
}
