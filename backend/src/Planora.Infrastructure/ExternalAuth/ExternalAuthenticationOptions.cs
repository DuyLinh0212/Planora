namespace Planora.Infrastructure.ExternalAuth;

public sealed class ExternalAuthenticationOptions
{
    public GoogleAuthenticationOptions Google { get; init; } = new();
    public FacebookAuthenticationOptions Facebook { get; init; } = new();
}

public sealed class GoogleAuthenticationOptions
{
    public string ClientId { get; init; } = string.Empty;
}

public sealed class FacebookAuthenticationOptions
{
    public string AppId { get; init; } = string.Empty;
}
