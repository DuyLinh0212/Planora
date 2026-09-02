namespace Planora.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "Planora.Api";
    public string Audience { get; init; } = "Planora.Frontends";
    public string Secret { get; init; } = string.Empty;
    // Refresh token expiry is a sliding inactivity timeout. Each successful refresh rotates it.
    public int AccessTokenMinutes { get; init; } = 60;
    public int RefreshTokenDays { get; init; } = 1;
}
