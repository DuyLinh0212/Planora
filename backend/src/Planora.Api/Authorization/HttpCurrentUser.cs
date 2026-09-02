using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Planora.Application.Common.Interfaces;

namespace Planora.Api.Authorization;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var subject = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(subject, out var userId) ? userId : null;
        }
    }
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
