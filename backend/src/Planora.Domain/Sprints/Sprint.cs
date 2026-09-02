using Planora.Domain.Common;

namespace Planora.Domain.Sprints;

public enum SprintStatus { Planned, Active, Closed, Cancelled }

public sealed class Sprint : AuditableEntity
{
    private Sprint() { }

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Goal { get; private set; }
    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset EndAt { get; private set; }
    public SprintStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static Sprint CreateSprint(Guid projectId, string name, string? goal, DateTimeOffset startAt, DateTimeOffset endAt, Guid createdByUserId, DateTimeOffset createdAt)
    {
        if (startAt >= endAt)
            throw new ArgumentException("Sprint start time must be before end time.", nameof(startAt));

        var sprint = new Sprint
        {
            ProjectId = projectId,
            Name = name.Trim(),
            Goal = goal?.Trim(),
            StartAt = startAt,
            EndAt = endAt,
            Status = SprintStatus.Planned,
            CreatedByUserId = createdByUserId
        };
        sprint.MarkCreated(createdAt);
        return sprint;
    }

    public BusinessRuleResult StartSprint(bool projectAlreadyHasActiveSprint, DateTimeOffset startedAt)
    {
        if (Status != SprintStatus.Planned)
            return BusinessRuleResult.Failure("sprint.not_planned", "Only a planned sprint can start.");
        if (projectAlreadyHasActiveSprint)
            return BusinessRuleResult.Failure("sprint.active_exists", "The project already has an active sprint.");

        Status = SprintStatus.Active;
        MarkUpdated(startedAt);
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult CloseSprint(DateTimeOffset closedAt)
    {
        if (Status != SprintStatus.Active)
            return BusinessRuleResult.Failure("sprint.not_active", "Only an active sprint can close.");

        Status = SprintStatus.Closed;
        ClosedAt = closedAt;
        MarkUpdated(closedAt);
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult UpdateSprint(string name, string? goal, DateTimeOffset startAt, DateTimeOffset endAt, DateTimeOffset updatedAt)
    {
        if (Status != SprintStatus.Planned)
            return BusinessRuleResult.Failure("sprint.not_planned", "Only a planned sprint can be edited.");
        if (startAt >= endAt)
            return BusinessRuleResult.Failure("sprint.invalid_period", "Sprint start time must be before end time.");
        Name = name.Trim();
        Goal = goal?.Trim();
        StartAt = startAt;
        EndAt = endAt;
        MarkUpdated(updatedAt);
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult CancelSprint(DateTimeOffset cancelledAt)
    {
        if (Status == SprintStatus.Closed)
            return BusinessRuleResult.Failure("sprint.closed", "A closed sprint cannot be cancelled.");
        Status = SprintStatus.Cancelled;
        DeletedAt = cancelledAt;
        MarkUpdated(cancelledAt);
        return BusinessRuleResult.Success();
    }

    public void RestoreSprint(DateTimeOffset restoredAt)
    {
        DeletedAt = null;
        Status = SprintStatus.Planned;
        ClosedAt = null;
        MarkUpdated(restoredAt);
    }
}
