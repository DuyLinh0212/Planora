using Planora.Domain.Common;

namespace Planora.Domain.Tasks;

public sealed class TaskAssignee
{
    private TaskAssignee() { }
    public Guid TaskId { get; private set; }
    public Guid ProjectMemberId { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public static TaskAssignee AssignProjectMemberToTask(Guid taskId, Guid projectMemberId, Guid assignedByUserId, DateTimeOffset assignedAt) => new() { TaskId = taskId, ProjectMemberId = projectMemberId, AssignedByUserId = assignedByUserId, AssignedAt = assignedAt };
}

public sealed class TaskAcceptanceCriterion : Entity
{
    private TaskAcceptanceCriterion() { }
    public Guid TaskId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public static TaskAcceptanceCriterion CreateTaskAcceptanceCriterion(Guid taskId, string content, int sortOrder) => new() { TaskId = taskId, Content = content.Trim(), SortOrder = sortOrder };
}
