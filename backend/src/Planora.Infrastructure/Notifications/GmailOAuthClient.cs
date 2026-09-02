using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;

namespace Planora.Infrastructure.Notifications;

/// <summary>
/// Talks to Google's token endpoint. The popup returns an authorization code; exchanging it
/// once yields the refresh token Planora stores, and every send trades that refresh token for
/// a short-lived access token so no long-lived credential is kept in memory.
/// </summary>
public sealed class GmailOAuthClient(HttpClient httpClient, IOptions<GmailIntegrationOptions> options) : IGmailOAuthClient
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
    private readonly GmailIntegrationOptions _integrationOptions = options.Value;

    public bool IsConfigured => _integrationOptions.IsConfigured;

    public async Task<ApplicationResult<GmailAuthorization>> ExchangeAuthorizationCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            return ApplicationResult.Failure<GmailAuthorization>(ApplicationErrors.External("gmail.not_configured", "Gmail sending is not configured on the server."));

        var response = await PostTokenRequestAsync(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _integrationOptions.ClientId,
            ["client_secret"] = _integrationOptions.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        }, cancellationToken);

        if (response is null)
            return ApplicationResult.Failure<GmailAuthorization>(ApplicationErrors.External("gmail.token_exchange_failed", "Google refused the Gmail authorization. Try linking again."));
        if (string.IsNullOrWhiteSpace(response.RefreshToken))
            return ApplicationResult.Failure<GmailAuthorization>(ApplicationErrors.Validation(
                "gmail.refresh_token_missing",
                "Google did not return a refresh token. Remove Planora from your Google account permissions and link again.",
                "code"));

        var gmailAddress = await ReadGoogleAccountEmailAsync(response.AccessToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(gmailAddress))
            return ApplicationResult.Failure<GmailAuthorization>(ApplicationErrors.External("gmail.profile_unavailable", "Could not read the Gmail address for this account."));

        return ApplicationResult.Success(new GmailAuthorization(gmailAddress, response.RefreshToken));
    }

    public async Task<ApplicationResult<string>> CreateAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            return ApplicationResult.Failure<string>(ApplicationErrors.External("gmail.not_configured", "Gmail sending is not configured on the server."));

        var response = await PostTokenRequestAsync(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = _integrationOptions.ClientId,
            ["client_secret"] = _integrationOptions.ClientSecret,
            ["grant_type"] = "refresh_token"
        }, cancellationToken);

        return response is null || string.IsNullOrWhiteSpace(response.AccessToken)
            ? ApplicationResult.Failure<string>(ApplicationErrors.External("gmail.refresh_rejected", "Google rejected the stored Gmail authorization."))
            : ApplicationResult.Success(response.AccessToken);
    }

    private async Task<TokenResponse?> PostTokenRequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        try
        {
            using var content = new FormUrlEncodedContent(form);
            using var response = await httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task<string?> ReadGoogleAccountEmailAsync(string? accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            var profile = await response.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken);
            return profile?.EmailVerified == true ? profile.Email : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string? AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string? RefreshToken);

    private sealed record GoogleUserInfo(
        [property: System.Text.Json.Serialization.JsonPropertyName("email")] string? Email,
        [property: System.Text.Json.Serialization.JsonPropertyName("email_verified")] bool EmailVerified);
}
