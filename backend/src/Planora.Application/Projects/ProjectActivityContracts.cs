namespace Planora.Application.Projects;

public sealed record ProjectActivityResponse(
    Guid Id,
    string Action,
    string EntityType,
    string EntityId,
    Guid? ActorUserId,
    string ActorDisplayName,
    DateTimeOffset CreatedAt);
