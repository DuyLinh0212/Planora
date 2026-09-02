namespace Planora.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }

    protected void MarkCreated(DateTimeOffset now)
    {
        CreatedAt = now;
        UpdatedAt = now;
    }

    protected void MarkUpdated(DateTimeOffset now) => UpdatedAt = now;
}

public readonly record struct BusinessRuleResult(bool IsSuccess, string? Code = null, string? Message = null)
{
    public static BusinessRuleResult Success() => new(true);
    public static BusinessRuleResult Failure(string code, string message) => new(false, code, message);
}
