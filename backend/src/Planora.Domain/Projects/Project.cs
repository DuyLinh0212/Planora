using Planora.Domain.Common;

namespace Planora.Domain.Projects;

public sealed class Project : AuditableEntity
{
    private Project() { }

    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset? StartAt { get; private set; }
    public DateTimeOffset? EndAt { get; private set; }
    public ProjectStatus Status { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static Project CreateProject(Guid ownerUserId, string name, string description, DateTimeOffset? startAt, DateTimeOffset? endAt, DateTimeOffset createdAt)
    {
        var project = new Project
        {
            OwnerUserId = ownerUserId,
            Name = name.Trim(),
            Description = description.Trim(),
            StartAt = startAt,
            EndAt = endAt,
            Status = ProjectStatus.Planning
        };
        project.MarkCreated(createdAt);
        return project;
    }

    public BusinessRuleResult UpdateProject(string name, string description, DateTimeOffset? startAt, DateTimeOffset? endAt, ProjectStatus? status, DateTimeOffset updatedAt)
    {
        if (startAt is not null && endAt is not null && startAt >= endAt)
            return BusinessRuleResult.Failure("project.invalid_period", "Project start time must be before end time.");

        Name = name.Trim();
        Description = description.Trim();
        StartAt = startAt;
        EndAt = endAt;
        if (status.HasValue)
        {
            Status = status.Value;
        }
        MarkUpdated(updatedAt);
        return BusinessRuleResult.Success();
    }

    public void DeleteProject(DateTimeOffset deletedAt)
    {
        DeletedAt = deletedAt;
        MarkUpdated(deletedAt);
    }

    public void RestoreProject(DateTimeOffset restoredAt)
    {
        DeletedAt = null;
        MarkUpdated(restoredAt);
    }
}
