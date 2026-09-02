using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Planora.Application.Billing;

namespace Planora.Infrastructure.Payments;

public sealed class MomoPaymentGateway(
    HttpClient httpClient,
    IOptions<MomoPaymentOptions> options) : IMomoPaymentGateway
{
    private readonly MomoPaymentOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<MomoCheckoutSession> CreateCheckoutAsync(MomoCheckoutRequest checkout, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            return new MomoCheckoutSession(null, "MoMo chưa được cấu hình trên máy chủ.");

        var amount = decimal.Truncate(checkout.Amount).ToString(CultureInfo.InvariantCulture);
        var requestId = checkout.OrderId;
        const string extraData = "";
        var rawSignature = $"accessKey={_options.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={_options.IpnUrl}&orderId={checkout.OrderId}&orderInfo={checkout.OrderInfo}&partnerCode={_options.PartnerCode}&redirectUrl={_options.ReturnUrl}&requestId={requestId}&requestType={_options.RequestType}";
        var payload = new MomoCreatePaymentRequest(
            _options.PartnerCode,
            requestId,
            checkout.OrderId,
            long.Parse(amount, CultureInfo.InvariantCulture),
            checkout.OrderInfo,
            _options.ReturnUrl,
            _options.IpnUrl,
            _options.RequestType,
            extraData,
            "vi",
            ComputeSignature(rawSignature));

        try
        {
            using var response = await httpClient.PostAsJsonAsync(_options.Endpoint, payload, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<MomoCreatePaymentResponse>(cancellationToken: cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new MomoCheckoutSession(null, "MoMo không thể tạo phiên thanh toán. Hãy thử lại sau.");
            if (body is null || body.ResultCode != 0 || string.IsNullOrWhiteSpace(body.PayUrl))
                return new MomoCheckoutSession(null, body?.Message ?? "MoMo không trả về liên kết thanh toán.");
            return new MomoCheckoutSession(body.PayUrl, null);
        }
        catch (HttpRequestException)
        {
            return new MomoCheckoutSession(null, "Không thể kết nối MoMo. Giao dịch chưa được ghi nhận; hãy thử lại.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new MomoCheckoutSession(null, "MoMo phản hồi quá lâu. Giao dịch chưa được ghi nhận; hãy thử lại.");
        }
    }

    public bool IsValidCallbackSignature(MomoPaymentCallback callback)
    {
        if (!IsConfigured || !string.Equals(callback.PartnerCode, _options.PartnerCode, StringComparison.Ordinal))
            return false;

        var amount = decimal.Truncate(callback.Amount).ToString(CultureInfo.InvariantCulture);
        var rawSignature = $"accessKey={_options.AccessKey}&amount={amount}&extraData={callback.ExtraData}&message={callback.Message}&orderId={callback.OrderId}&orderInfo={callback.OrderInfo}&orderType={callback.OrderType ?? string.Empty}&partnerCode={callback.PartnerCode}&payType={callback.PayType ?? string.Empty}&requestId={callback.RequestId}&responseTime={callback.ResponseTime}&resultCode={callback.ResultCode}&transId={callback.TransId ?? string.Empty}";
        var expected = Encoding.UTF8.GetBytes(ComputeSignature(rawSignature));
        var actual = Encoding.UTF8.GetBytes(callback.Signature ?? string.Empty);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private string ComputeSignature(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
        return Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private sealed record MomoCreatePaymentRequest(
        string PartnerCode,
        string RequestId,
        string OrderId,
        long Amount,
        string OrderInfo,
        string RedirectUrl,
        string IpnUrl,
        string RequestType,
        string ExtraData,
        string Lang,
        string Signature);

    private sealed record MomoCreatePaymentResponse(
        [property: JsonPropertyName("resultCode")] int ResultCode,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("payUrl")] string? PayUrl);
}
