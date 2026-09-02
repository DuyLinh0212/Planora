using Planora.Domain.Common;

namespace Planora.Domain.Projects;

public sealed class ProjectMember : Entity
{
    private ProjectMember() { }

    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public MembershipStatus Status { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    public static ProjectMember AddProjectMember(Guid projectId, Guid userId, DateTimeOffset joinedAt) => new()
    {
        ProjectId = projectId,
        UserId = userId,
        Status = MembershipStatus.Active,
        JoinedAt = joinedAt
    };

    public BusinessRuleResult RemoveProjectMember(bool memberIsProjectOwner)
    {
        if (memberIsProjectOwner)
            return BusinessRuleResult.Failure("project.owner_cannot_leave", "Transfer project ownership before removing the owner.");

        Status = MembershipStatus.Removed;
        return BusinessRuleResult.Success();
    }
}
