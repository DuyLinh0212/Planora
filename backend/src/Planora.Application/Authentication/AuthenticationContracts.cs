namespace Planora.Application.Authentication;

public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string DisplayName,
    string? DeviceInfo,
    string? Username = null,
    bool AcceptedTerms = false,
    bool RememberMe = false);
public sealed record LoginUserRequest(
    string? Identifier,
    string Password,
    string? DeviceInfo,
    bool RememberMe = false,
    string? Email = null)
{
    public string EffectiveIdentifier => !string.IsNullOrWhiteSpace(Identifier)
        ? Identifier
        : (!string.IsNullOrWhiteSpace(Email) ? Email : string.Empty);
}
public sealed record ExternalLoginRequest(string Provider, string Token, string? DeviceInfo, bool RememberMe = false);
public sealed record RefreshAccessTokenRequest(string? RefreshToken, string? DeviceInfo, bool RememberMe = false);
public sealed record LogoutUserRequest(string? RefreshToken);
public sealed record RequestPasswordResetRequest(string Email);
public sealed record RequestPasswordResetResponse(string Message, string? ResetToken, DateTimeOffset? ExpiresAt = null);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record AuthenticationResponse(
    Guid UserId,
    string Email,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    [property: System.Text.Json.Serialization.JsonIgnore] string RefreshToken);
