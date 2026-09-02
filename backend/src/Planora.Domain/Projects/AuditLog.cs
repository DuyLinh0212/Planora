using Planora.Domain.Common;

namespace Planora.Domain.Projects;

public sealed class AuditLog : Entity
{
    private AuditLog() { }
    public Guid? ActorUserId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditLog CreateAuditLog(Guid? actorUserId, Guid? projectId, string action, string entityType, string entityId, string? beforeJson, string? afterJson, string? ipAddress, DateTimeOffset createdAt) => new()
    {
        ActorUserId = actorUserId,
        ProjectId = projectId,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        BeforeJson = beforeJson,
        AfterJson = afterJson,
        IpAddress = ipAddress,
        CreatedAt = createdAt
    };
}
