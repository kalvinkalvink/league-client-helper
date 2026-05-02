using LolClientHelper.Models;

namespace LolClientHelper.Services;

public interface IGameStateService
{
    GameState CurrentState { get; }
    bool IsRunning { get; }

    event EventHandler<GameState>? GameStateChanged;
    event EventHandler? ApiConfigured;

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
