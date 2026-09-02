using Planora.Domain.Projects;

namespace Planora.Application.ProjectMembers;

public sealed record InviteProjectMemberRequest(string Email, Guid RoleId, int ExpiresInDays = 7);
public sealed record ProjectInvitationResponse(
    Guid Id,
    string Email,
    InvitationStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    string InvitationToken);
public sealed record ProjectMemberResponse(Guid MembershipId, Guid UserId, string DisplayName, string Email, MembershipStatus Status, IReadOnlyList<string> Roles);
public sealed record ChangeProjectMemberRoleRequest(Guid RoleId);
public sealed record RemoveProjectMemberRequest(string Reason);
public sealed record RegisteredUserMatchResponse(Guid UserId, string DisplayName, string Email, string? AvatarUrl, bool IsAlreadyMember);
public sealed record ProjectInvitationSummaryResponse(Guid Id, string Email, InvitationStatus Status, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt);
public sealed record ProjectRoleOptionResponse(Guid Id, string Code, string Name, bool IsSystemRole);
