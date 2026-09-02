using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;
using Planora.Infrastructure.Notifications;

namespace Planora.IntegrationTests;

public sealed class SmtpTaskEmailNotificationSenderTests
{
    [Fact]
    public async Task SendTaskNotification_PresentsTheActorAsAuthorAndRoutesRepliesToThem()
    {
        await using var smtpSink = SmtpMessageSink.Start();
        var sender = CreateSender(new TaskEmailNotificationOptions
        {
            FrontendBaseUrl = "http://localhost:4200/",
            SmtpHost = "127.0.0.1",
            SmtpPort = smtpSink.Port,
            EnableSsl = false,
            FromAddress = "no-reply@planora.app",
            FromNameSuffix = "via Planora"
        });

        await sender.SendTaskNotificationAsync(CreateNotification(), CancellationToken.None);
        var payload = await smtpSink.WaitForMessageAsync(TimeSpan.FromSeconds(10));

        Assert.Contains("no-reply@planora.app", payload, StringComparison.Ordinal);
        Assert.Contains("via Planora", payload, StringComparison.Ordinal);
        Assert.Contains("Reply-To:", payload, StringComparison.Ordinal);
        Assert.Contains("abs@gmail.com", payload, StringComparison.Ordinal);
        Assert.Contains("b.member@gmail.com", payload, StringComparison.Ordinal);
        Assert.Contains("http://localhost:4200/projects/", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendTaskNotification_WithoutSmtpHost_DoesNotThrow()
    {
        var sender = CreateSender(new TaskEmailNotificationOptions());

        await sender.SendTaskNotificationAsync(CreateNotification(), CancellationToken.None);
    }

    private static SmtpTaskEmailNotificationSender CreateSender(TaskEmailNotificationOptions options) =>
        new(Options.Create(options), NullLogger<SmtpTaskEmailNotificationSender>.Instance);

    private static TaskEmailNotification CreateNotification() => new(
        Guid.CreateVersion7(),
        "Nguyen A",
        "abs@gmail.com",
        "b.member@gmail.com",
        "Tran B",
        "[Planora] Ban duoc giao cong viec \"Thiet ke ERD\"",
        "Noi dung cong viec.",
        "/projects/6f9619ff-8b86-d011-b42d-00cf4fc964ff/tasks");
}
