using LolClientHelper.Models;

namespace LolClientHelper.Services;

public interface IWebSocketService
{
    bool IsConnected { get; }

    event EventHandler<string>? MessageReceived;

    Task ConnectAsync(LcuCredentials credentials, CancellationToken ct = default);
    Task SubscribeAsync(string topic, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task StartReceivingAsync(CancellationToken ct = default);
}
