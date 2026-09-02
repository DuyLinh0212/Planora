using Planora.Domain.Common;

namespace Planora.Domain.Administration;

public sealed class SystemSetting : Entity
{
    private SystemSetting() { }

    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public Guid? UpdatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static SystemSetting CreateSystemSetting(string key, string value, Guid? updatedByUserId, DateTimeOffset updatedAt) => new()
    {
        Key = key.Trim().ToUpperInvariant(),
        Value = value.Trim(),
        UpdatedByUserId = updatedByUserId,
        UpdatedAt = updatedAt
    };

    public void UpdateSystemSetting(string value, Guid? updatedByUserId, DateTimeOffset updatedAt)
    {
        Value = value.Trim();
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = updatedAt;
    }
}
