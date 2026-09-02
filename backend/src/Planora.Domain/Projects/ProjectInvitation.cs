using Planora.Domain.Common;

namespace Planora.Domain.Projects;

public sealed class ProjectInvitation : Entity
{
    private ProjectInvitation() { }

    public Guid ProjectId { get; private set; }
    public string InvitedEmail { get; private set; } = string.Empty;
    public Guid? InvitedUserId { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public Guid RoleId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public InvitationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ProjectInvitation CreateProjectInvitation(Guid projectId, string invitedEmail, Guid? invitedUserId, Guid invitedByUserId, Guid roleId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt) => new()
    {
        ProjectId = projectId,
        InvitedEmail = invitedEmail.Trim(),
        InvitedUserId = invitedUserId,
        InvitedByUserId = invitedByUserId,
        RoleId = roleId,
        TokenHash = tokenHash,
        ExpiresAt = expiresAt,
        Status = InvitationStatus.Pending,
        CreatedAt = createdAt
    };

    public BusinessRuleResult AcceptProjectInvitation(DateTimeOffset respondedAt)
    {
        if (Status != InvitationStatus.Pending)
            return BusinessRuleResult.Failure("invitation.not_pending", "Invitation is no longer pending.");
        if (ExpiresAt <= respondedAt)
        {
            Status = InvitationStatus.Expired;
            return BusinessRuleResult.Failure("invitation.expired", "Invitation has expired.");
        }

        Status = InvitationStatus.Accepted;
        RespondedAt = respondedAt;
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult RejectProjectInvitation(DateTimeOffset respondedAt)
    {
        if (Status != InvitationStatus.Pending)
            return BusinessRuleResult.Failure("invitation.not_pending", "Invitation is no longer pending.");

        Status = InvitationStatus.Rejected;
        RespondedAt = respondedAt;
        return BusinessRuleResult.Success();
    }
}
