using System.Net.Http.Headers;
using System.Net.Http.Json;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;

namespace Planora.Infrastructure.ExternalAuth;

public sealed class ExternalIdentityVerifier(
    HttpClient httpClient,
    IOptions<ExternalAuthenticationOptions> options,
    ILogger<ExternalIdentityVerifier> logger) : IExternalIdentityVerifier
{
    private readonly ExternalAuthenticationOptions _authenticationOptions = options.Value;

    public Task<ApplicationResult<ExternalIdentity>> VerifyExternalIdentityAsync(string provider, string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult(ApplicationResult.Failure<ExternalIdentity>(ApplicationErrors.Validation("identity.external_token_required", "External identity token is required.", "token")));

        return provider.Trim().ToUpperInvariant() switch
        {
            "GOOGLE" => VerifyGoogleIdentityAsync(token, cancellationToken),
            "FACEBOOK" => VerifyFacebookIdentityAsync(token, cancellationToken),
            _ => Task.FromResult(ApplicationResult.Failure<ExternalIdentity>(ApplicationErrors.Validation("identity.external_provider_unsupported", "Supported providers are Google and Facebook.", "provider")))
        };
    }

    private async Task<ApplicationResult<ExternalIdentity>> VerifyGoogleIdentityAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_authenticationOptions.Google.ClientId))
            return ApplicationResult.Failure<ExternalIdentity>(ApplicationErrors.External("identity.google_not_configured", "Google login is not configured."));
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = await GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_authenticationOptions.Google.ClientId],
                // Google tokens were being rejected as "not yet valid" because this development
                // machine's Windows clock can drift by a little over one minute. Keep all token
                // checks enabled while allowing a small, bounded issued-at skew.
                IssuedAtClockTolerance = TimeSpan.FromMinutes(2)
            });
            if (string.IsNullOrWhiteSpace(payload.Email))
                return ApplicationResult.Failure<ExternalIdentity>(ApplicationErrors.Unauthorized("Google account did not provide an email address."));
            return ApplicationResult.Success(new ExternalIdentity(payload.Subject, payload.Email, payload.Name ?? payload.Email, payload.Picture));
        }
        catch (InvalidJwtException exception)
        {
            logger.LogWarning(
                exception,
                "Google identity token validation failed. ExpectedClientIdSuffix={ClientIdSuffix}, TokenLength={TokenLength}",
                _authenticationOptions.Google.ClientId.Length > 12
                    ? _authenticationOptions.Google.ClientId[^12..]
                    : _authenticationOptions.Google.ClientId,
                token.Length);
            return ApplicationResult.Failure<ExternalIdentity>(ApplicationErrors.Unauthorized("Google identity token is invalid."));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApplicationResult.Failure<ExternalIdentity>(ApplicationErrors.External("identity.google_timeout", "Google identity verification timed out. Try again."));
        }
    }

    private async Task<ApplicationResult<ExternalIdentity>> VerifyFacebookIdentityAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_authenticationOptions.Facebook.AppId))
            return ApplicationResult.Failure<ExternalIdentity>(ApplicationErrors.External("identity.facebook_not_configured", "Facebook login is not configured."));

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.facebook.com/me?fields=id,name,email,picture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return ApplicationResult.Failure<ExternalIdentity>(ApplicationErrors.Unauthorized("Facebook access token is invalid."));

        var profile = await response.Content.ReadFromJsonAsync<FacebookProfile>(cancellationToken);
        if (profile is null || string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Email))
            return ApplicationResult.Failure<ExternalIdentity>(ApplicationErrors.Unauthorized("Facebook account did not provide a verified email address."));
        return ApplicationResult.Success(new ExternalIdentity(profile.Id, profile.Email, profile.Name ?? profile.Email, profile.Picture?.Data?.Url));
    }

    private sealed record FacebookProfile(string Id, string? Name, string? Email, FacebookPicture? Picture);
    private sealed record FacebookPicture(FacebookPictureData? Data);
    private sealed record FacebookPictureData(string? Url);
}
