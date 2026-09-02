using Planora.Domain.Projects;

namespace Planora.UnitTests;

public sealed class ProjectMembershipTests
{
    [Fact]
    public void RemoveProjectOwnerRequiresOwnershipTransfer()
    {
        var member = ProjectMember.AddProjectMember(Guid.CreateVersion7(), Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        var removalResult = member.RemoveProjectMember(memberIsProjectOwner: true);
        Assert.False(removalResult.IsSuccess);
        Assert.Equal("project.owner_cannot_leave", removalResult.Code);
        Assert.Equal(MembershipStatus.Active, member.Status);
    }

    [Fact]
    public void RejectProjectInvitationPreventsLaterAcceptance()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var invitation = ProjectInvitation.CreateProjectInvitation(Guid.CreateVersion7(), "member@example.com", null, Guid.CreateVersion7(), Guid.CreateVersion7(), "hash", currentTime.AddDays(7), currentTime);
        var rejectionResult = invitation.RejectProjectInvitation(currentTime.AddMinutes(1));
        var acceptanceResult = invitation.AcceptProjectInvitation(currentTime.AddMinutes(2));
        Assert.True(rejectionResult.IsSuccess);
        Assert.False(acceptanceResult.IsSuccess);
        Assert.Equal(InvitationStatus.Rejected, invitation.Status);
    }
}
