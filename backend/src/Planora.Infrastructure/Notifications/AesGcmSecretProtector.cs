using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;

namespace Planora.Infrastructure.Notifications;

/// <summary>
/// AES-GCM protection for Gmail refresh tokens. Each value gets a fresh 96-bit nonce, and the
/// authentication tag is appended to the cipher text so tampering fails the unwrap.
/// </summary>
public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public AesGcmSecretProtector(IOptions<GmailIntegrationOptions> options)
    {
        var configuredKey = options.Value.TokenEncryptionKey;
        _key = string.IsNullOrWhiteSpace(configuredKey) ? [] : Convert.FromBase64String(configuredKey);
        if (_key.Length is not (0 or 16 or 24 or 32))
            throw new InvalidOperationException("GmailIntegration:TokenEncryptionKey must be a base64 encoded 16, 24, or 32 byte key.");
    }

    public (string Cipher, string Nonce) Protect(string plainText)
    {
        EnsureKeyConfigured();
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length + TagSize];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes.AsSpan(0, plainBytes.Length), cipherBytes.AsSpan(plainBytes.Length));
        return (Convert.ToBase64String(cipherBytes), Convert.ToBase64String(nonce));
    }

    public string Unprotect(string cipher, string nonce)
    {
        EnsureKeyConfigured();
        var cipherBytes = Convert.FromBase64String(cipher);
        if (cipherBytes.Length < TagSize)
            throw new CryptographicException("Stored secret is shorter than the authentication tag.");

        var plainBytes = new byte[cipherBytes.Length - TagSize];
        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(
            Convert.FromBase64String(nonce),
            cipherBytes.AsSpan(0, plainBytes.Length),
            cipherBytes.AsSpan(plainBytes.Length),
            plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private void EnsureKeyConfigured()
    {
        if (_key.Length == 0)
            throw new InvalidOperationException("GmailIntegration:TokenEncryptionKey is not configured.");
    }
}
