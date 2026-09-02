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

    public string CreatePaymentReference(Guid paymentId) => $"{NormalisedPrefix}{paymentId:N}";

    public BankTransferInstructionsResponse GetInstructions(string transferContent) => new(
        _options.BankName.Trim(),
        _options.AccountName.Trim(),
        _options.AccountNumber.Trim(),
        transferContent,
        string.IsNullOrWhiteSpace(_options.Branch) ? null : _options.Branch.Trim());

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

    private Regex PaymentReferenceRegex() => new($"{Regex.Escape(NormalisedPrefix)}[0-9A-F]{{32}}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
}
