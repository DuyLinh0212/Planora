namespace Planora.Domain.Projects;

public enum ProjectStatus { Planning, Active, Paused, Completed, Cancelled }
public enum MembershipStatus { Active, Removed }
public enum InvitationStatus { Pending, Accepted, Rejected, Expired }
public enum PermissionEffect { Allow, Deny }

public static class DefaultProjectRoleCodes
{
    public const string Owner = "OWNER";
    public const string Leader = "LEADER";
    public const string Member = "MEMBER";
    public const string Viewer = "VIEWER";
}
