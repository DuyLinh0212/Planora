namespace Planora.Infrastructure.Storage;

public sealed class CloudinaryOptions
{
    public string CloudName { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public string RootFolder { get; init; } = "planora";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(CloudName) && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ApiSecret);
}

public sealed class StorageOptions
{
    public int MaxFileSizeMb { get; init; } = 25;
}
