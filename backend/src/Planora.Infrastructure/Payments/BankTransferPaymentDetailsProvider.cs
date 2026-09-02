using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Planora.Application.Billing;

namespace Planora.Infrastructure.Payments;

public sealed class BankTransferPaymentDetailsProvider(IOptions<BankTransferPaymentOptions> options) : IBankTransferPaymentDetailsProvider
{
    private readonly BankTransferPaymentOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    // VietQR limits the transfer description to 25 characters. 20 hexadecimal characters
    // give 80 bits of uniqueness while leaving room for the standard PLN prefix.
    public string CreatePaymentReference(Guid paymentId) => $"{NormalisedPrefix}{paymentId:N}".ToUpperInvariant()[..Math.Min(25, NormalisedPrefix.Length + 20)];

    public BankTransferInstructionsResponse GetInstructions(string transferContent, decimal amount)
    {
        var bankName = _options.BankName.Trim();
        var accountName = _options.AccountName.Trim();
        var accountNumber = _options.AccountNumber.Trim();
        var bankId = string.IsNullOrWhiteSpace(_options.VietQrBankId) ? bankName : _options.VietQrBankId.Trim();
        var qrCodeUrl = $"https://img.vietqr.io/image/{Uri.EscapeDataString(bankId)}-{Uri.EscapeDataString(accountNumber)}-compact2.png" +
                        $"?amount={amount.ToString("0", System.Globalization.CultureInfo.InvariantCulture)}" +
                        $"&addInfo={Uri.EscapeDataString(transferContent)}" +
                        $"&accountName={Uri.EscapeDataString(accountName)}";

        return new BankTransferInstructionsResponse(
            bankName,
            accountName,
            accountNumber,
            transferContent,
            string.IsNullOrWhiteSpace(_options.Branch) ? null : _options.Branch.Trim(),
            qrCodeUrl);
    }

    public string? ExtractPaymentReference(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;
        var match = PaymentReferenceRegex().Match(content);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    public bool IsExpectedDestinationAccount(string accountNumber) =>
        !string.IsNullOrWhiteSpace(accountNumber) &&
        string.Equals(accountNumber.Trim(), _options.AccountNumber.Trim(), StringComparison.Ordinal);

    public bool IsValidWebhookAuthorization(string? authorizationHeader)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(authorizationHeader))
            return false;
        var expected = Encoding.UTF8.GetBytes($"Apikey {_options.SePayWebhookApiKey}");
        var actual = Encoding.UTF8.GetBytes(authorizationHeader.Trim());
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private string NormalisedPrefix => Regex.Replace(_options.PaymentReferencePrefix.Trim().ToUpperInvariant(), "[^A-Z0-9]", string.Empty) switch
    {
        { Length: > 0 } prefix => prefix,
        _ => "PLN"
    };

    // Accept both 20-character references created now and the previous 32-character
    // UUID references so callbacks for already-issued payment instructions still work.
    private Regex PaymentReferenceRegex() => new($"{Regex.Escape(NormalisedPrefix)}[0-9A-F]{{20}}(?:[0-9A-F]{{12}})?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
}
