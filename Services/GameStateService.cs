using System.Text.Json;
using LolClientHelper.Models;

namespace LolClientHelper.Services;

public sealed class GameStateService : IGameStateService, IDisposable
{
    private const string LogSource = "GameStateService";
    private const string TopicAllEvents = "OnJsonApiEvent";
    private const int MaxPollingFailuresBeforeRefresh = 5;

    private readonly ILoggingService _log;
    private readonly ISettingsService _settings;
    private readonly ILeagueProcessService _leagueProcessService;
    private readonly ILcuApiService _lcuApiService;
    private readonly IWebSocketService _webSocketService;
    private CancellationTokenSource? _runCts;
    private Task? _pollingTask;
    private Task? _wsReceiveTask;
    private bool _disposed;
    private int _consecutivePollingFailures;

    public GameStateService(
        ILoggingService log,
        ISettingsService settings,
        ILeagueProcessService leagueProcessService,
        ILcuApiService lcuApiService,
        IWebSocketService webSocketService)
    {
        _log = log;
        _settings = settings;
        _leagueProcessService = leagueProcessService;
        _lcuApiService = lcuApiService;
        _webSocketService = webSocketService;
    }

    public GameState CurrentState { get; private set; } = GameState.Unknown;
    public bool IsRunning => _runCts is not null;
    public event EventHandler<GameState>? GameStateChanged;
    public event EventHandler? ApiConfigured;
    public event EventHandler? InvitationReceived;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_runCts is not null)
            return;

        _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var runToken = _runCts.Token;
        _webSocketService.MessageReceived += OnWebSocketMessageReceived;
        InvitationReceived += OnInvitationReceived;

        await InitializeLcuApiServiceAsync(runToken).ConfigureAwait(false);

        var wsConnected = await TryConnectWebSocketAsync(_lcuApiService.Credentials, runToken).ConfigureAwait(false);
        if (!wsConnected)
        {
            _log.Warning(LogSource, "WebSocket unavailable, using HTTP polling fallback.");
        }

        _pollingTask = RunPollingLoopAsync(runToken);
    }

    private async Task InitializeLcuApiServiceAsync(CancellationToken ct)
    {
        var credentials = await _leagueProcessService.WaitForCredentialsAsync(ct: ct).ConfigureAwait(false);
        _lcuApiService.Configure(credentials);
        ApiConfigured?.Invoke(this, EventArgs.Empty);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_runCts is null)
            return;

        _runCts.Cancel();
        _webSocketService.MessageReceived -= OnWebSocketMessageReceived;
        InvitationReceived -= OnInvitationReceived;

        var tasks = new[] { _pollingTask, _wsReceiveTask }.Where(t => t is not null).Cast<Task>().ToArray();
        if (tasks.Length > 0)
        {
            try
            {
                await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
            }
            catch
            {
                // Best effort shutdown.
            }
        }

        await _webSocketService.DisconnectAsync(ct).ConfigureAwait(false);
        _runCts.Dispose();
        _runCts = null;
        _pollingTask = null;
        _wsReceiveTask = null;
    }

    private async Task<bool> TryConnectWebSocketAsync(LcuCredentials credentials, CancellationToken ct)
    {
        try
        {
            await _webSocketService.ConnectAsync(credentials, ct).ConfigureAwait(false);
            await _webSocketService.SubscribeAsync(TopicAllEvents, ct).ConfigureAwait(false);

            _wsReceiveTask = Task.Run(() => _webSocketService.StartReceivingAsync(ct), ct);
            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(LogSource, $"WebSocket connect/subscribe failed: {ex.Message}");
            return false;
        }
    }

    private async Task RunPollingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var interval = Math.Max(100, _settings.Current.PollIntervalMs);

            try
            {
                var phase = await _lcuApiService.GetGameflowPhaseRawAsync(ct).ConfigureAwait(false);
                var state = ParseGameState(phase);
                UpdateState(state);
                _consecutivePollingFailures = 0;
            }
            catch (Exception ex)
            {
                _consecutivePollingFailures++;
                _log.Warning(LogSource, $"Polling failed ({_consecutivePollingFailures}/{MaxPollingFailuresBeforeRefresh}): {ex.Message}");

                if (_consecutivePollingFailures >= MaxPollingFailuresBeforeRefresh)
                {
                    _log.Warning(LogSource, "Max consecutive failures reached, re-fetching LCU credentials...");
                    try
                    {
                        await InitializeLcuApiServiceAsync(ct).ConfigureAwait(false);
                        _log.Info(LogSource, $"Re-configured LCU API with new credentials (port {_lcuApiService.Credentials?.Port})");
                        _consecutivePollingFailures = 0;
                    }
                    catch (Exception reFetchEx)
                    {
                        _log.Error(LogSource, "Failed to re-fetch credentials", reFetchEx);
                    }
                }
            }

            try
            {
                await Task.Delay(interval, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void OnWebSocketMessageReceived(object? sender, string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
            return;
        Console.WriteLine($"Raw Sender {sender}");
        try
        {
            using var doc = JsonDocument.Parse(rawMessage);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() < 3)
                return;

            var msgType = doc.RootElement[0].GetInt32();
            if (msgType != 8)
                return;

            var topic = doc.RootElement[1].GetString();
            if (!string.Equals(topic, "OnJsonApiEvent", StringComparison.OrdinalIgnoreCase))
                return;

            var eventPayload = doc.RootElement[2];
            if (!eventPayload.TryGetProperty("uri", out var uriElement))
                return;

            var uri = uriElement.GetString() ?? string.Empty;
            if (uri.Contains("/lol-gameflow/v1/gameflow-phase", StringComparison.OrdinalIgnoreCase) &&
                eventPayload.TryGetProperty("data", out var dataElement) &&
                dataElement.ValueKind == JsonValueKind.String)
            {
                var state = ParseGameState(dataElement.GetString() ?? string.Empty);
                UpdateState(state);
            }

            if (uri.Contains("/lol-lobby/v2/received-invitations", StringComparison.OrdinalIgnoreCase))
            {
                _log.Debug(LogSource, $"WS: Received invitation event");
                InvitationReceived?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _log.Debug(LogSource, $"Ignored WS message: {ex.Message}");
        }
    }

    private async void OnInvitationReceived(object? sender, EventArgs e)
    {
        if (!_settings.Current.AutoAcceptInvite)
            return;

        //try { await Task.Delay(500).ConfigureAwait(false); }
        //catch { return; }

        var invitations = await _lcuApiService.GetReceivedInvitationsAsync().ConfigureAwait(false);
        foreach (var inv in invitations)
        {
            await _lcuApiService.AcceptInvitationAsync(inv.InvitationId).ConfigureAwait(false);
            _log.Info(LogSource, $"Accepted invitation from {inv.InvitationId}");
        }
    }

    private void UpdateState(GameState nextState)
    {
        if (nextState == CurrentState)
            return;

        var previous = CurrentState;
        CurrentState = nextState;
        _log.Info(LogSource, $"State changed: {previous} -> {nextState}");
        GameStateChanged?.Invoke(this, nextState);

        if (nextState == GameState.ReadyCheck && _settings.Current.AutoAcceptMatch)
        {
            _log.Info(LogSource, "Auto-accepting ready check...");
            _ = _lcuApiService.AcceptReadyCheckAsync().ConfigureAwait(false);
        }

        if (nextState == GameState.PreEndOfGame && _settings.Current.AutoSkipLike)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    await _lcuApiService.SkipHonorAsync().ConfigureAwait(false);
                    _log.Info(LogSource, "Skipped honor");
                }
                catch (Exception ex) { _log.Warning(LogSource, $"Skip honor failed: {ex.Message}"); }
            });
        }

        if (nextState == GameState.EndOfGame && _settings.Current.AutoReenterLobby)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    await _lcuApiService.PlayAgainAsync().ConfigureAwait(false);
                    _log.Info(LogSource, "Play again queued");
                }
                catch (Exception ex) { _log.Warning(LogSource, $"Play again failed: {ex.Message}"); }
            });
        }
    }

    private static GameState ParseGameState(string raw)
    {
        return raw.Trim('"') switch
        {
            "None" => GameState.MainMenu,
            "Lobby" => GameState.Lobby,
            "Matchmaking" => GameState.Matchmaking,
            "ReadyCheck" => GameState.ReadyCheck,
            "ChampSelect" => GameState.ChampSelect,
            "InProgress" => GameState.InProgress,
            "PreEndOfGame" => GameState.PreEndOfGame,
            "WaitingForStats" => GameState.WaitingForStats,
            "EndOfGame" => GameState.EndOfGame,
            "Reconnect" => GameState.Reconnect,
            _ => GameState.Unknown
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_runCts is not null)
        {
            try { StopAsync().GetAwaiter().GetResult(); } catch { }
        }
    }
}
