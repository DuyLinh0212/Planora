namespace Planora.Infrastructure.Payments;

public sealed class MomoPaymentOptions
{
    public string PartnerCode { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Endpoint { get; init; } = "https://test-payment.momo.vn/v2/gateway/api/create";
    public string ReturnUrl { get; init; } = string.Empty;
    public string IpnUrl { get; init; } = string.Empty;
    public string RequestType { get; init; } = "captureWallet";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PartnerCode) &&
        !string.IsNullOrWhiteSpace(AccessKey) &&
        !string.IsNullOrWhiteSpace(SecretKey) &&
        Uri.TryCreate(Endpoint, UriKind.Absolute, out _) &&
        Uri.TryCreate(ReturnUrl, UriKind.Absolute, out _) &&
        Uri.TryCreate(IpnUrl, UriKind.Absolute, out _);
}

public sealed class BankTransferPaymentOptions
{
    public string BankName { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string? Branch { get; init; }
    public string PaymentReferencePrefix { get; init; } = "PLN";
    public string SePayWebhookApiKey { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BankName) &&
        !string.IsNullOrWhiteSpace(AccountName) &&
        !string.IsNullOrWhiteSpace(AccountNumber) &&
        !string.IsNullOrWhiteSpace(SePayWebhookApiKey);
}
