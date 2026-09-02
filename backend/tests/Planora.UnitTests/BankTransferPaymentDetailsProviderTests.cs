using Microsoft.Extensions.Options;
using Planora.Infrastructure.Payments;

namespace Planora.UnitTests;

public sealed class BankTransferPaymentDetailsProviderTests
{
    [Fact]
    public void GetInstructions_BuildsDynamicVietQrWithPaymentAmountAndReference()
    {
        var provider = new BankTransferPaymentDetailsProvider(Options.Create(new BankTransferPaymentOptions
        {
            BankName = "ACB",
            VietQrBankId = "970416",
            AccountName = "NGUYEN DUY LINH",
            AccountNumber = "36142087",
            SePayWebhookApiKey = "test-key"
        }));

        const string reference = "PLN01A062CD1B857BCF90D9";
        var instructions = provider.GetInstructions(reference, 149_000m);

        Assert.Equal(
            "https://img.vietqr.io/image/970416-36142087-compact2.png?amount=149000&addInfo=PLN01A062CD1B857BCF90D9&accountName=NGUYEN%20DUY%20LINH",
            instructions.QrCodeUrl);
    }

    [Fact]
    public void PaymentReferences_FitVietQrTransferContentLimit_AndRecognizeLegacyReferences()
    {
        var provider = new BankTransferPaymentDetailsProvider(Options.Create(new BankTransferPaymentOptions
        {
            BankName = "ACB",
            AccountName = "NGUYEN DUY LINH",
            AccountNumber = "36142087",
            SePayWebhookApiKey = "test-key"
        }));

        var reference = provider.CreatePaymentReference(Guid.Parse("01a062cd-1b85-7bcf-90d9-547f0a937b60"));

        Assert.Equal("PLN01A062CD1B857BCF90D9", reference);
        Assert.True(reference.Length <= 25);
        Assert.Equal(reference, provider.ExtractPaymentReference($"Thanh toan {reference}"));
        Assert.Equal("PLN01A062CD1B857BCF90D9547F0A937B60", provider.ExtractPaymentReference("PLN01A062CD1B857BCF90D9547F0A937B60"));
    }
}
