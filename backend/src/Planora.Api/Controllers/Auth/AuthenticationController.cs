using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Planora.Api.Extensions;
using Planora.Application.Authentication;
using Planora.Application.Common.Results;

namespace Planora.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(
    AuthenticationService authenticationService) : ControllerBase
{
    private const string RefreshTokenCookieName = "planora.user.refresh";
    private const string PersistentLoginCookieName = "planora.user.persistent";

    [HttpPost("register")]
    [EnableRateLimiting("auth-registration")]
    public async Task<IResult> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RegisterUserAsync(request, cancellationToken);
        SetSessionCookies(result, request.RememberMe);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IResult> LoginUserAsync(LoginUserRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginUserAsync(request, cancellationToken);
        SetSessionCookies(result, request.RememberMe);
        return result.ToHttpResult();
    }

    [HttpPost("external/{provider}")]
    [EnableRateLimiting("auth-login")]
    public async Task<IResult> LoginWithExternalProviderAsync(string provider, ExternalLoginBody request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginWithExternalProviderAsync(new ExternalLoginRequest(provider, request.Token, request.DeviceInfo, request.RememberMe), cancellationToken);
        SetSessionCookies(result, request.RememberMe);
        return result.ToHttpResult();
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-refresh")]
    public async Task<IResult> RefreshAccessTokenAsync(RefreshAccessTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? request.RefreshToken;
        var result = await authenticationService.RefreshAccessTokenAsync(request with { RefreshToken = refreshToken }, cancellationToken);
        SetSessionCookies(result, IsPersistentLoginCookie() || request.RememberMe);
        return result.ToHttpResult();
    }

    [HttpPost("logout")]
    public async Task<IResult> LogoutUserAsync(LogoutUserRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? request.RefreshToken;
        var result = await authenticationService.LogoutUserAsync(request with { RefreshToken = refreshToken }, cancellationToken);
        DeleteSessionCookies();
        return result.ToHttpResult();
    }

    [HttpPost("password/forgot")]
    [EnableRateLimiting("auth-password")]
    public async Task<IResult> RequestPasswordResetAsync(RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RequestPasswordResetAsync(request, cancellationToken);
        if (result.IsFailure || result.Value is null)
            return result.ToHttpResult();

        var response = new RequestPasswordResetResponse(
            result.Value.Message,
            null,
            result.Value.ExpiresAt);
        return Results.Json(response, statusCode: StatusCodes.Status202Accepted);
    }

    [HttpPost("password/reset")]
    [EnableRateLimiting("auth-password")]
    public async Task<IResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ResetPasswordAsync(request, cancellationToken);
        return result.ToHttpResult();
    }

    [Authorize]
    [HttpPost("password/change")]
    public async Task<IResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ChangePasswordAsync(request, cancellationToken);
        return result.ToHttpResult();
    }

    private void SetSessionCookies(ApplicationResult<AuthenticationResponse> result, bool rememberMe)
    {
        if (result.IsFailure || result.Value is null) return;

        var isDevelopment = HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
        var refreshCookie = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = !isDevelopment,
            Path = "/api/auth"
        };
        var persistenceCookie = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = !isDevelopment,
            Path = "/api/auth"
        };

        if (rememberMe)
        {
            var expires = DateTimeOffset.UtcNow.AddDays(1);
            refreshCookie.Expires = expires;
            persistenceCookie.Expires = expires;
        }

        Response.Cookies.Append(RefreshTokenCookieName, result.Value.RefreshToken, refreshCookie);
        Response.Cookies.Append(PersistentLoginCookieName, rememberMe ? "1" : "0", persistenceCookie);
    }

    private bool IsPersistentLoginCookie() => Request.Cookies[PersistentLoginCookieName] == "1";

    private void DeleteSessionCookies()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/auth" });
        Response.Cookies.Delete(PersistentLoginCookieName, new CookieOptions { Path = "/api/auth" });
    }

    public sealed record ExternalLoginBody(string Token, string? DeviceInfo, bool RememberMe = false);
}
