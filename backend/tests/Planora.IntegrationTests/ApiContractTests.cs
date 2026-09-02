using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Planora.IntegrationTests;

public sealed class ApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _applicationFactory;
    private readonly HttpClient _httpClient;

    public ApiContractTests(WebApplicationFactory<Program> applicationFactory)
    {
        _applicationFactory = applicationFactory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        _httpClient = _applicationFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task RootEndpointDescribesPlanoraApiAsync()
    {
        var response = await _httpClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ServiceDescriptor>();
        Assert.Equal("Planora API", payload?.Service);
    }

    [Fact]
    public async Task ProjectEndpointsRequireAuthenticationAsync()
    {
        var response = await _httpClient.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocumentIsAvailableAsync()
    {
        var response = await _httpClient.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRegistrationReturnsProblemDetailsAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "invalid-email",
            Password = "short",
            DisplayName = "",
            DeviceInfo = "integration-test"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDescriptor>();
        Assert.Equal("identity.invalid_email", problemDetails?.Code);
    }

    [Fact]
    public async Task GmailRegistrationRequiresAcceptedTermsAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "planora.user@gmail.com",
            Username = "planora.user",
            Password = "Strong-pass-2026",
            DisplayName = "Planora User",
            AcceptedTerms = false,
            DeviceInfo = "integration-test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDescriptor>();
        Assert.Equal("identity.terms_required", problemDetails?.Code);
    }

    [Fact]
    public async Task RegistrationPasswordRequiresSpecialCharacterAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "planora.user@gmail.com",
            Username = "planora.user",
            Password = "Strongpass2026",
            DisplayName = "Planora User",
            AcceptedTerms = true,
            DeviceInfo = "integration-test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDescriptor>();
        Assert.Equal("identity.weak_password", problemDetails?.Code);
    }

    [Fact]
    public async Task InvalidEmailPasswordRecoveryStillReturnsAcceptedAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/password/forgot", new
        {
            Email = "not-an-email"
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task WeakResetPasswordReturnsValidationProblemAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/password/reset", new
        {
            Token = "unused-token",
            NewPassword = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDescriptor>();
        Assert.Equal("identity.weak_password", problemDetails?.Code);
    }

    [Fact]
    public async Task ChangePasswordWithoutAuthenticationReturnsUnauthorizedAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/password/change", new
        {
            CurrentPassword = "old-password",
            NewPassword = "new-password-123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisteredAccountCanLoginWithUsernameAndEmailAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"planora.login.{suffix}@gmail.com";
        var username = $"user_{suffix}";
        const string password = "Strong-pass-2026";

        var registration = await _httpClient.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Username = username,
            Password = password,
            DisplayName = "Login Contract User",
            AcceptedTerms = true,
            DeviceInfo = "integration-test"
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        AuthenticationDescriptor? authentication = null;
        foreach (var identifier in new[] { username, email })
        {
            var login = await _httpClient.PostAsJsonAsync("/api/auth/login", new
            {
                Identifier = identifier,
                Password = password,
                DeviceInfo = "integration-test"
            });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            authentication = await login.Content.ReadFromJsonAsync<AuthenticationDescriptor>();
            Assert.False(string.IsNullOrWhiteSpace(authentication?.AccessToken));
            Assert.Contains(login.Headers, header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
        }

        using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authentication!.AccessToken);
        var profileResponse = await _httpClient.SendAsync(profileRequest);
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileDescriptor>();
        Assert.Equal(authentication.UserId, profile?.UserId);
        Assert.False(profile?.GmailLink.IsLinked);
        Assert.NotNull(profile?.GmailLink);

        using var preferencesRequest = new HttpRequestMessage(HttpMethod.Put, "/api/profile/preferences")
        {
            Content = JsonContent.Create(new
            {
                PreferredLanguage = "vi",
                ThemePreference = "calm",
                TimeZoneId = "Asia/Bangkok",
                EmailTaskNotificationsEnabled = true
            })
        };
        preferencesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
        var preferencesResponse = await _httpClient.SendAsync(preferencesRequest);
        Assert.Equal(HttpStatusCode.NoContent, preferencesResponse.StatusCode);

        using var updatedProfileRequest = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        updatedProfileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
        var updatedProfileResponse = await _httpClient.SendAsync(updatedProfileRequest);
        var updatedProfile = await updatedProfileResponse.Content.ReadFromJsonAsync<ProfileDescriptor>();
        Assert.True(updatedProfile?.EmailTaskNotificationsEnabled);
    }

    [Fact]
    public async Task RefreshTrafficDoesNotConsumeLoginRateLimitAsync()
    {
        await using var isolatedFactory = _applicationFactory.WithWebHostBuilder(_ => { });
        using var client = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var invalidLogin = await client.PostAsJsonAsync("/api/auth/login", new
            {
                Identifier = $"missing-user-{attempt}",
                Password = "Wrong-pass-2026!",
                DeviceInfo = "rate-limit-test"
            });
            Assert.Equal(HttpStatusCode.Unauthorized, invalidLogin.StatusCode);
        }

        var limitedLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Identifier = "one-attempt-too-many",
            Password = "Wrong-pass-2026!",
            DeviceInfo = "rate-limit-test"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, limitedLogin.StatusCode);
        Assert.NotNull(limitedLogin.Headers.RetryAfter);
        var rateLimitProblem = await limitedLogin.Content.ReadFromJsonAsync<ProblemDescriptor>();
        Assert.Equal("authentication_rate_limited", rateLimitProblem?.Code);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            RefreshToken = "invalid-refresh-token",
            DeviceInfo = "rate-limit-test"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/overview")]
    [InlineData("/api/admin/accounts")]
    [InlineData("/api/admin/plans")]
    [InlineData("/api/admin/payments")]
    [InlineData("/api/admin/feedback")]
    [InlineData("/api/admin/analytics")]
    [InlineData("/api/admin/activity")]
    [InlineData("/api/admin/support/conversations")]
    public async Task GetAdminResource_WithoutAuthentication_ReturnsUnauthorized(string resourcePath)
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync(resourcePath);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record ServiceDescriptor(string Service);
    private sealed record ProblemDescriptor(string Code);
    private sealed record AuthenticationDescriptor(Guid UserId, string AccessToken);
    private sealed record GmailLinkDescriptor(bool IsLinked, bool IsServerConfigured);
    private sealed record ProfileDescriptor(Guid UserId, GmailLinkDescriptor GmailLink, bool EmailTaskNotificationsEnabled);
}
