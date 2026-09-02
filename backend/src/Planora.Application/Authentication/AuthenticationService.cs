using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Users;

namespace Planora.Application.Authentication;

public sealed class AuthenticationService(
    IPlanoraDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    IExternalIdentityVerifier externalIdentityVerifier,
    IPasswordResetNotificationSender passwordResetNotificationSender,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    int refreshTokenLifetimeDays)
{
    public async Task<ApplicationResult<AuthenticationResponse>> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var validationError = ValidateRegistrationRequest(request);
        if (validationError is not null)
            return ApplicationResult.Failure<AuthenticationResponse>(validationError);

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
            return ApplicationResult.Failure<AuthenticationResponse>(ApplicationErrors.Conflict("identity.email_exists", "An account with this email already exists."));

        var username = request.Username!.Trim();
        var normalizedUsername = NormalizeUsername(username);
        if (await dbContext.Users.AnyAsync(user => user.NormalizedUsername == normalizedUsername, cancellationToken))
            return ApplicationResult.Failure<AuthenticationResponse>(ApplicationErrors.Conflict("identity.username_exists", "This username is already in use."));

        var currentTime = timeProvider.GetUtcNow();
        var user = User.CreateUser(request.Email, normalizedEmail, username, normalizedUsername, request.DisplayName, currentTime, currentTime);
        user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password), currentTime);
        dbContext.Users.Add(user);

        return await IssueAuthenticationTokensAsync(user, request.DeviceInfo, currentTime, cancellationToken);
    }

    public async Task<ApplicationResult<AuthenticationResponse>> LoginUserAsync(LoginUserRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.EffectiveIdentifier;
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password))
            return ApplicationResult.Failure<AuthenticationResponse>(ApplicationErrors.Unauthorized("Email, username, or password is incorrect."));

        var normalizedIdentifier = identifier.Trim().ToUpperInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedIdentifier || candidate.NormalizedUsername == normalizedIdentifier,
            cancellationToken);
        if (user is null || user.PasswordHash is null || !passwordHasher.VerifyPassword(user, user.PasswordHash, request.Password))
            return ApplicationResult.Failure<AuthenticationResponse>(ApplicationErrors.Unauthorized("Email, username, or password is incorrect."));
        if (user.Status != UserStatus.Active)
            return ApplicationResult.Failure<AuthenticationResponse>(ApplicationErrors.Forbidden("identity.account_inactive", "This account is not active."));

        return await IssueAuthenticationTokensAsync(user, request.DeviceInfo, timeProvider.GetUtcNow(), cancellationToken);
    }

    public async Task<ApplicationResult<AuthenticationResponse>> LoginWithExternalProviderAsync(ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        var verifiedIdentity = await externalIdentityVerifier.VerifyExternalIdentityAsync(request.Provider, request.Token, cancellationToken);
        if (verifiedIdentity.IsFailure || verifiedIdentity.Value is null)
            return ApplicationResult.Failure<AuthenticationResponse>(verifiedIdentity.Errors.ToArray());

        var normalizedProvider = request.Provider.Trim().ToUpperInvariant();
        var identity = verifiedIdentity.Value;
        var existingExternalLogin = await dbContext.ExternalLogins
            .FirstOrDefaultAsync(login => login.Provider == normalizedProvider && login.ProviderUserId == identity.ProviderUserId, cancellationToken);

        User? user = existingExternalLogin is null
            ? await dbContext.Users.FirstOrDefaultAsync(candidate => candidate.NormalizedEmail == NormalizeEmail(identity.Email), cancellationToken)
            : await dbContext.Users.FindAsync([existingExternalLogin.UserId], cancellationToken);

        var currentTime = timeProvider.GetUtcNow();
        if (user is null)
        {
            var username = await CreateAvailableUsernameAsync(identity.Email, cancellationToken);
            user = User.CreateUser(identity.Email, NormalizeEmail(identity.Email), username, NormalizeUsername(username), identity.DisplayName, currentTime, currentTime);
            user.UpdateUserProfile(identity.DisplayName, identity.AvatarUrl, currentTime);
            dbContext.Users.Add(user);
        }

        if (existingExternalLogin is null)
            dbContext.ExternalLogins.Add(ExternalLogin.CreateExternalLogin(user.Id, normalizedProvider, identity.ProviderUserId, currentTime));

        return await IssueAuthenticationTokensAsync(user, request.DeviceInfo, currentTime, cancellationToken);
    }

    public async Task<ApplicationResult<AuthenticationResponse>> RefreshAccessTokenAsync(RefreshAccessTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return ApplicationResult.Failure<AuthenticationResponse>(ApplicationErrors.Unauthorized("Refresh token is invalid or expired."));

        var currentTime = timeProvider.GetUtcNow();
        var refreshTokenHash = tokenIssuer.HashOpaqueToken(request.RefreshToken);
        var storedRefreshToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == refreshTokenHash, cancellationToken);
        if (storedRefreshToken is null || !storedRefreshToken.IsRefreshTokenActive(currentTime))
            return ApplicationResult.Failure<AuthenticationResponse>(ApplicationErrors.Unauthorized("Refresh token is invalid or expired."));

        var user = await dbContext.Users.FindAsync([storedRefreshToken.UserId], cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
            return ApplicationResult.Failure<AuthenticationResponse>(ApplicationErrors.Unauthorized("Account is unavailable."));

        storedRefreshToken.RevokeRefreshToken(currentTime);
        return await IssueAuthenticationTokensAsync(user, request.DeviceInfo, currentTime, cancellationToken);
    }

    public async Task<ApplicationResult> LogoutUserAsync(LogoutUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return ApplicationResult.Success();

        var refreshTokenHash = tokenIssuer.HashOpaqueToken(request.RefreshToken);
        var storedRefreshToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == refreshTokenHash, cancellationToken);
        if (storedRefreshToken is not null)
        {
            storedRefreshToken.RevokeRefreshToken(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<RequestPasswordResetResponse>> RequestPasswordResetAsync(
        RequestPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        const string responseMessage = "If the account exists, a password reset code has been sent.";
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal))
            return ApplicationResult.Success(new RequestPasswordResetResponse(responseMessage, null));

        var user = await dbContext.Users.FirstOrDefaultAsync(
            candidate => candidate.NormalizedEmail == NormalizeEmail(request.Email),
            cancellationToken);
        if (user is null || user.Status != UserStatus.Active || user.PasswordHash is null)
            return ApplicationResult.Success(new RequestPasswordResetResponse(responseMessage, null));

        var currentTime = timeProvider.GetUtcNow();
        var existingTokens = await dbContext.PasswordResetTokens
            .Where(token => token.UserId == user.Id && token.UsedAt == null && token.ExpiresAt > currentTime)
            .ToListAsync(cancellationToken);
        foreach (var existingToken in existingTokens)
            existingToken.MarkPasswordResetTokenUsed(currentTime);

        var resetCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var resetCodeHash = tokenIssuer.HashOpaqueToken(resetCode);
        var expiresAt = currentTime.AddMinutes(15);
        dbContext.PasswordResetTokens.Add(PasswordResetToken.CreatePasswordResetToken(
            user.Id,
            resetCodeHash,
            expiresAt,
            currentTime));
        await dbContext.SaveChangesAsync(cancellationToken);
        await passwordResetNotificationSender.SendPasswordResetInstructionsAsync(
            user.Email,
            user.DisplayName,
            resetCode,
            expiresAt,
            cancellationToken);

        return ApplicationResult.Success(new RequestPasswordResetResponse(responseMessage, resetCode, expiresAt));
    }

    public async Task<ApplicationResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var passwordError = ValidateNewPassword(request.NewPassword);
        if (passwordError is not null)
            return ApplicationResult.Failure(passwordError);
        if (string.IsNullOrWhiteSpace(request.Token))
            return ApplicationResult.Failure(ApplicationErrors.Validation(
                "identity.invalid_password_reset_token",
                "The password reset link is invalid or expired.",
                "token"));

        var currentTime = timeProvider.GetUtcNow();
        var tokenHash = tokenIssuer.HashOpaqueToken(request.Token);
        var passwordResetToken = await dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (passwordResetToken is null || !passwordResetToken.IsPasswordResetTokenActive(currentTime))
            return ApplicationResult.Failure(ApplicationErrors.Validation(
                "identity.invalid_password_reset_token",
                "The password reset link is invalid or expired.",
                "token"));

        var user = await dbContext.Users.FindAsync([passwordResetToken.UserId], cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
            return ApplicationResult.Failure(ApplicationErrors.Validation(
                "identity.invalid_password_reset_token",
                "The password reset link is invalid or expired.",
                "token"));

        user.SetPasswordHash(passwordHasher.HashPassword(user, request.NewPassword), currentTime);
        passwordResetToken.MarkPasswordResetTokenUsed(currentTime);
        await RevokeAllRefreshTokensAsync(user.Id, currentTime, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());

        var passwordError = ValidateNewPassword(request.NewPassword);
        if (passwordError is not null)
            return ApplicationResult.Failure(passwordError);

        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null || user.Status != UserStatus.Active || user.PasswordHash is null)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());
        if (!passwordHasher.VerifyPassword(user, user.PasswordHash, request.CurrentPassword))
            return ApplicationResult.Failure(ApplicationErrors.Validation(
                "identity.current_password_incorrect",
                "Current password is incorrect.",
                "currentPassword"));
        if (passwordHasher.VerifyPassword(user, user.PasswordHash, request.NewPassword))
            return ApplicationResult.Failure(ApplicationErrors.Validation(
                "identity.password_unchanged",
                "New password must be different from the current password.",
                "newPassword"));

        var currentTime = timeProvider.GetUtcNow();
        user.SetPasswordHash(passwordHasher.HashPassword(user, request.NewPassword), currentTime);
        await RevokeAllRefreshTokensAsync(user.Id, currentTime, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationResult<AuthenticationResponse>> IssueAuthenticationTokensAsync(User user, string? deviceInfo, DateTimeOffset issuedAt, CancellationToken cancellationToken)
    {
        var accessToken = tokenIssuer.CreateAccessToken(user, issuedAt);
        var refreshToken = tokenIssuer.CreateOpaqueToken();
        dbContext.RefreshTokens.Add(RefreshToken.CreateRefreshToken(user.Id, refreshToken.Hash, issuedAt.AddDays(refreshTokenLifetimeDays), deviceInfo, issuedAt));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(new AuthenticationResponse(user.Id, user.Email, user.Username, user.DisplayName, user.AvatarUrl, accessToken.Value, accessToken.ExpiresAt, refreshToken.Value));
    }

    private static ApplicationError? ValidateRegistrationRequest(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            return ApplicationErrors.Validation("identity.invalid_email", "Registration requires a valid @gmail.com address.", "email");
        if (!request.AcceptedTerms)
            return ApplicationErrors.Validation("identity.terms_required", "Accept the terms of use before creating an account.", "acceptedTerms");
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length is < 3 or > 30 || request.Username.Any(character => !char.IsLetterOrDigit(character) && character is not '_' and not '.'))
            return ApplicationErrors.Validation("identity.invalid_username", "Username must be 3-30 characters and contain only letters, numbers, dots, or underscores.", "username");
        var passwordError = ValidatePassword(request.Password, "password");
        if (passwordError is not null)
            return passwordError;
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return ApplicationErrors.Validation("identity.display_name_required", "Display name is required.", "displayName");
        return null;
    }

    private static ApplicationError? ValidateNewPassword(string password)
    {
        return ValidatePassword(password, "newPassword");
    }

    private static ApplicationError? ValidatePassword(string password, string field)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 9 || !password.Any(character => !char.IsLetterOrDigit(character)))
            return ApplicationErrors.Validation(
                "identity.weak_password",
                "Password must contain at least 9 characters and one special character.",
                field);
        return null;
    }

    private async Task<string> CreateAvailableUsernameAsync(string email, CancellationToken cancellationToken)
    {
        var stem = new string(email.Split('@', 2)[0].Where(character => char.IsLetterOrDigit(character) || character is '_' or '.').ToArray());
        if (stem.Length < 3)
            stem = $"user{stem}";
        stem = stem[..Math.Min(stem.Length, 24)];
        var candidate = stem;
        var suffix = 1;
        while (await dbContext.Users.AnyAsync(user => user.NormalizedUsername == NormalizeUsername(candidate), cancellationToken))
            candidate = $"{stem}{suffix++}";
        return candidate;
    }

    private async Task RevokeAllRefreshTokensAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        var activeRefreshTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > revokedAt)
            .ToListAsync(cancellationToken);
        foreach (var refreshToken in activeRefreshTokens)
            refreshToken.RevokeRefreshToken(revokedAt);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
    private static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();
}
