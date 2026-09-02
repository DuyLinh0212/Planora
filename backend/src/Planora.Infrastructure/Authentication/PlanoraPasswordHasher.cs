using Microsoft.AspNetCore.Identity;
using Planora.Application.Common.Interfaces;
using Planora.Domain.Users;

namespace Planora.Infrastructure.Authentication;

public sealed class PlanoraPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password) => _passwordHasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string passwordHash, string password) =>
        _passwordHasher.VerifyHashedPassword(user, passwordHash, password) != PasswordVerificationResult.Failed;
}
