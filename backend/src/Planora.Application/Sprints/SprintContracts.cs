using Planora.Domain.Sprints;

namespace Planora.Application.Sprints;

public sealed record CreateSprintRequest(string Name, string? Goal, DateTimeOffset StartAt, DateTimeOffset EndAt);
public sealed record UpdateSprintRequest(string Name, string? Goal, DateTimeOffset StartAt, DateTimeOffset EndAt);
public sealed record SprintResponse(Guid Id, Guid ProjectId, string Name, string? Goal, DateTimeOffset StartAt, DateTimeOffset EndAt, SprintStatus Status);
