using System.Net.WebSockets;
using System.Text;
using LolClientHelper.Models;

namespace LolClientHelper.Services;

public sealed class WebSocketService : IWebSocketService, IDisposable
{
    private const string LogSource = "WebSocketService";
    private readonly ILoggingService _log;
    private ClientWebSocket? _socket;
    private bool _disposed;

    public WebSocketService(ILoggingService log)
    {
        _log = log;
    }

    public bool IsConnected => _socket?.State == WebSocketState.Open;
    public event EventHandler<string>? MessageReceived;

    public async Task ConnectAsync(LcuCredentials credentials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        await DisconnectAsync(ct).ConfigureAwait(false);

        _socket = new ClientWebSocket();
        _socket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        _socket.Options.SetRequestHeader("Authorization", $"Basic {credentials.BasicAuthHeader}");

        var wsUri = new Uri($"wss://127.0.0.1:{credentials.Port}/");
        await _socket.ConnectAsync(wsUri, ct).ConfigureAwait(false);

        // WAMP hello handshake required before subscriptions.
        await SendRawAsync("[1,\"wamp\",2,{\"roles\":{}}]", ct).ConfigureAwait(false);
        _log.Info(LogSource, $"Connected to WAMP socket at {wsUri}");
    }

    public Task SubscribeAsync(string topic, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic is required.", nameof(topic));

        return SendRawAsync($"[5,\"{topic}\"]", ct);
    }

    public async Task StartReceivingAsync(CancellationToken ct = default)
    {
        if (_socket is null)
            throw new InvalidOperationException("WebSocket is not connected.");

        var buffer = new byte[8192];

        while (!ct.IsCancellationRequested && _socket.State == WebSocketState.Open)
        {
            var segment = new ArraySegment<byte>(buffer);
            using var ms = new MemoryStream();

            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(segment, ct).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _log.Warning(LogSource, "Socket close frame received.");
                    return;
                }

                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            var raw = Encoding.UTF8.GetString(ms.ToArray());
            MessageReceived?.Invoke(this, raw);
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_socket is null)
            return;

        try
        {
            if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutdown", ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(LogSource, $"WebSocket disconnect failed: {ex.Message}");
        }
        finally
        {
            _socket.Dispose();
            _socket = null;
        }
    }

    private async Task SendRawAsync(string payload, CancellationToken ct)
    {
        if (_socket is null || _socket.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not connected.");

        var bytes = Encoding.UTF8.GetBytes(payload);
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        _log.Debug(LogSource, $"WS -> {payload}");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _socket?.Dispose();
        _socket = null;
    }
}
