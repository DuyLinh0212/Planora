using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Planora.IntegrationTests;

/// <summary>
/// Minimal SMTP sink that accepts one message and exposes the raw DATA payload. It lets the
/// email tests assert real envelope headers without depending on an external mail server.
/// </summary>
internal sealed class SmtpMessageSink : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Task<string> _receiveTask;

    private SmtpMessageSink(TcpListener listener)
    {
        _listener = listener;
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        _receiveTask = ReceiveSingleMessageAsync();
    }

    public int Port { get; }

    public static SmtpMessageSink Start()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new SmtpMessageSink(listener);
    }

    public async Task<string> WaitForMessageAsync(TimeSpan timeout)
    {
        var completed = await Task.WhenAny(_receiveTask, Task.Delay(timeout));
        Assert.Same(_receiveTask, completed);
        return await _receiveTask;
    }

    private async Task<string> ReceiveSingleMessageAsync()
    {
        using var client = await _listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\r\n" };

        await writer.WriteLineAsync("220 planora-test ESMTP");
        var payload = new StringBuilder();
        var readingData = false;

        while (await reader.ReadLineAsync() is string line)
        {
            if (readingData)
            {
                if (line == ".")
                {
                    readingData = false;
                    await writer.WriteLineAsync("250 OK queued");
                    continue;
                }
                payload.AppendLine(line);
                continue;
            }

            if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                await writer.WriteLineAsync("250 planora-test");
            else if (line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase) || line.StartsWith("RCPT TO", StringComparison.OrdinalIgnoreCase))
                await writer.WriteLineAsync("250 OK");
            else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
            {
                readingData = true;
                await writer.WriteLineAsync("354 Start mail input; end with <CRLF>.<CRLF>");
            }
            else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 Bye");
                break;
            }
            else
            {
                await writer.WriteLineAsync("250 OK");
            }
        }

        return payload.ToString();
    }

    public ValueTask DisposeAsync()
    {
        _listener.Stop();
        return ValueTask.CompletedTask;
    }
}
