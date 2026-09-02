using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Planora.Application.Common.Interfaces;
using Planora.Domain.Users;

namespace Planora.Infrastructure.Authentication;

public sealed class JwtTokenIssuer(IOptions<JwtOptions> options) : ITokenIssuer
{
    private readonly JwtOptions _jwtOptions = options.Value;

    public AccessToken CreateAccessToken(User user, DateTimeOffset issuedAt)
    {
        if (Encoding.UTF8.GetByteCount(_jwtOptions.Secret) < 64)
            throw new InvalidOperationException("Jwt:Secret must contain at least 64 bytes.");

        var expiresAt = issuedAt.AddMinutes(Math.Clamp(_jwtOptions.AccessTokenMinutes, 5, 120));
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
        ];
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_jwtOptions.Issuer, _jwtOptions.Audience, claims, issuedAt.UtcDateTime, expiresAt.UtcDateTime, signingCredentials);
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }

    public OpaqueToken CreateOpaqueToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var tokenValue = Base64UrlEncoder.Encode(randomBytes);
        return new OpaqueToken(tokenValue, HashOpaqueToken(tokenValue));
    }

    public string HashOpaqueToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
