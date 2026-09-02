namespace Planora.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? IpAddress { get; }
}
