using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Planora.Infrastructure.Notifications;

namespace Planora.UnitTests;

public sealed class GmailIntegrationTests
{
    private static readonly GmailIntegrationOptions ConfiguredOptions = new()
    {
        ClientId = "planora-web.apps.googleusercontent.com",
        ClientSecret = "test-client-secret",
        TokenEncryptionKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray())
    };

    [Fact]
    public void SecretProtector_RoundTripsRefreshToken()
    {
        var protector = new AesGcmSecretProtector(Options.Create(ConfiguredOptions));

        var protectedToken = protector.Protect("refresh-token-from-google");

        Assert.NotEqual("refresh-token-from-google", protectedToken.Cipher);
        Assert.Equal("refresh-token-from-google", protector.Unprotect(protectedToken.Cipher, protectedToken.Nonce));
    }

    [Fact]
    public async Task ExchangeAuthorizationCode_UsesOpenIdUserInfoForVerifiedEmailAsync()
    {
        var requests = new List<CapturedRequest>();
        var handler = new StubHttpMessageHandler(async request =>
        {
            requests.Add(new CapturedRequest(
                request.RequestUri!,
                request.Headers.Authorization,
                request.Content is null ? null : await request.Content.ReadAsStringAsync()));

            return request.RequestUri!.AbsoluteUri switch
            {
                "https://oauth2.googleapis.com/token" => JsonResponse("""
                    {"access_token":"access-token","refresh_token":"refresh-token"}
                    """),
                "https://openidconnect.googleapis.com/v1/userinfo" => JsonResponse("""
                    {"email":"abs@gmail.com","email_verified":true}
                    """),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });
        var client = new GmailOAuthClient(new HttpClient(handler), Options.Create(ConfiguredOptions));

        var result = await client.ExchangeAuthorizationCodeAsync(
            "authorization-code",
            "http://localhost:4200",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("abs@gmail.com", result.Value?.GmailAddress);
        Assert.Equal("refresh-token", result.Value?.RefreshToken);
        Assert.Equal(2, requests.Count);
        Assert.Contains("redirect_uri=http%3A%2F%2Flocalhost%3A4200", requests[0].Body, StringComparison.Ordinal);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), requests[1].Authorization);
    }

    [Fact]
    public async Task ExchangeAuthorizationCode_RejectsUnverifiedEmailAsync()
    {
        var handler = new StubHttpMessageHandler(request => Task.FromResult(
            request.RequestUri!.AbsoluteUri.EndsWith("/token", StringComparison.Ordinal)
                ? JsonResponse("""{"access_token":"access-token","refresh_token":"refresh-token"}""")
                : JsonResponse("""{"email":"abs@gmail.com","email_verified":false}""")));
        var client = new GmailOAuthClient(new HttpClient(handler), Options.Create(ConfiguredOptions));

        var result = await client.ExchangeAuthorizationCodeAsync(
            "authorization-code",
            "http://localhost:4200",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == "gmail.profile_unavailable");
    }

    [Fact]
    public async Task CreateAccessToken_WhenGoogleIsUnavailable_ReturnsFailureAsync()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("network unavailable"));
        var client = new GmailOAuthClient(new HttpClient(handler), Options.Create(ConfiguredOptions));

        var result = await client.CreateAccessTokenAsync("refresh-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == "gmail.refresh_rejected");
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed record CapturedRequest(Uri Uri, AuthenticationHeaderValue? Authorization, string? Body);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
