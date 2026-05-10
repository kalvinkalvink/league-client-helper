using System.Linq;
using System.Text.Json;
using LolClientHelper.Models;

namespace LolClientHelper.Services;

public sealed class GameStateService : IGameStateService, IDisposable
{
    private const string LogSource = "GameStateService";
    private const string TopicAllEvents = "OnJsonApiEvent";
    private const int MaxPollingFailuresBeforeRefresh = 5;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
    private string? _currentSummonerPuuid;
    private readonly HashSet<string> _recentPartyJoins = new();
    private DateTime _lastPartyJoinTime = DateTime.MinValue;

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
    public event Action<string, string, string>? FriendGameStatusChanged;

    private readonly Dictionary<string, string> _friendGameStatuses = new();
    private readonly Dictionary<string, string> _friendProducts = new();

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

        // Fetch current summoner's puuid to filter out own events
        try
        {
            var summoner = await _lcuApiService.GetCurrentSummonerAsync(ct).ConfigureAwait(false);
            _currentSummonerPuuid = summoner?.Puuid;
            _log.Info(LogSource, $"Current summoner puuid: {_currentSummonerPuuid}");
        }
        catch (Exception ex)
        {
            _log.Warning(LogSource, $"Failed to get current summoner: {ex.Message}");
        }
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
                if (state == GameState.Unknown)
                    _log.Debug(LogSource, $"Unknown game state parsed: {phase}");
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
        _log.Debug(LogSource, $"Raw Sender {sender}");
        if (_log.IsDebugEnabled)
        {
            var truncated = rawMessage.Length > 500 ? rawMessage[..500] + "..." : rawMessage;
            _log.Debug(LogSource, $"Raw WebSocket message: {truncated}");
        }
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

            // parse game state
            var uri = uriElement.GetString() ?? string.Empty;
            if (uri.Contains("/lol-gameflow/v1/gameflow-phase", StringComparison.OrdinalIgnoreCase) &&
                eventPayload.TryGetProperty("data", out var dataElement) &&
                dataElement.ValueKind == JsonValueKind.String)
            {
                var state = ParseGameState(dataElement.GetString() ?? string.Empty);
                if (state == GameState.Unknown)
                    _log.Debug(LogSource, $"Unknown game state parsed: {dataElement.GetString()}");
                UpdateState(state);
            }




            if (uri.Contains("/lol-lobby/v2/received-invitations", StringComparison.OrdinalIgnoreCase))
            {
                _log.Debug(LogSource, $"WS: Received invitation event");
                InvitationReceived?.Invoke(this, EventArgs.Empty);
            }

            // Handle friend info events for auto-join party
            if (uri.Contains("/lol-hovercard/v1/friend-info/", StringComparison.OrdinalIgnoreCase))
            {
                _log.Debug(LogSource, $"WS: Friend info event received for {uri}");
                ProcessFriendInfoEvent(eventPayload);
            }
        }
        catch (Exception ex)
        {
            _log.Debug(LogSource, $"Ignored WS message: {ex.Message}");
        }
    }

    private void ProcessFriendInfoEvent(JsonElement eventPayload)
    {
        try
        {
            // Check if auto-join is enabled
            if (!_settings.Current.AutoJoinFriendParty)
                return;

            // Deserialize the event payload
            var payload = JsonSerializer.Deserialize<FriendInfoEventPayload>(eventPayload.GetRawText(), JsonOptions);
            if (payload == null)
                return;

            // Check event type is Update
            if (!string.Equals(payload.EventType, "Update", StringComparison.OrdinalIgnoreCase))
                return;

            var friendData = payload.Data;
            var friendPuuid = friendData.Puuid;

            // Filter out current summoner's own events
            if (!string.IsNullOrEmpty(_currentSummonerPuuid) && 
                string.Equals(friendPuuid, _currentSummonerPuuid, StringComparison.OrdinalIgnoreCase))
            {
                _log.Debug(LogSource, "Skipping own friend info event");
                return;
            }

            // Check if lol info exists
            var lolInfo = friendData.Lol;
            if (lolInfo == null)
            {
                _log.Debug(LogSource, $"Friend {friendData.GameName}: lol info is NULL");
                return;
            }

            // Debug: log what we received
            _log.Debug(LogSource, $"Friend {friendData.GameName}: availability={friendData.Availability}, gameStatus=[{lolInfo.GameStatus}], product={friendData.Product}");

            // Track game status AND product for UI updates
            var gameStatusChanged = false;
            var productChanged = false;

            if (!string.IsNullOrEmpty(lolInfo.GameStatus))
            {
                var previousStatus = _friendGameStatuses.TryGetValue(friendPuuid, out var status) ? status : string.Empty;
                if (!string.Equals(previousStatus, lolInfo.GameStatus, StringComparison.OrdinalIgnoreCase))
                {
                    _log.Debug(LogSource, $"FIRING event: {friendPuuid} gameStatus={lolInfo.GameStatus}");
                    _friendGameStatuses[friendPuuid] = lolInfo.GameStatus;
                    gameStatusChanged = true;
                }
            }

            // Track product from friend data
            if (!string.IsNullOrEmpty(friendData.Product))
            {
                var previousProduct = _friendProducts.TryGetValue(friendPuuid, out var prod) ? prod : string.Empty;
                if (!string.Equals(previousProduct, friendData.Product, StringComparison.OrdinalIgnoreCase))
                {
                    _friendProducts[friendPuuid] = friendData.Product;
                    productChanged = true;
                }
            }

            // Fire event if either changed
            if (gameStatusChanged || productChanged)
            {
                var currentStatus = _friendGameStatuses.TryGetValue(friendPuuid, out var s) ? s : string.Empty;
                var currentProduct = _friendProducts.TryGetValue(friendPuuid, out var p) ? p : string.Empty;
                FriendGameStatusChanged?.Invoke(friendPuuid, currentStatus, currentProduct);
            }

            // Parse party info from pty string
            if (string.IsNullOrEmpty(lolInfo.Pty))
                return;

            var partyInfo = JsonSerializer.Deserialize<PartyInfo>(lolInfo.Pty, JsonOptions);
            if (partyInfo == null)
                return;

            // Check if party is open and has a valid party ID
            if (!partyInfo.IsPartyOpen || string.IsNullOrEmpty(partyInfo.PartyId))
                return;

            // Check if this friend is in the selected list (only if list is not empty)
            // If no friends selected, allow random joins to any friend's party
            var selectedPuuids = _settings.Current.SelectedFriendPuuids;
            if (selectedPuuids != null && selectedPuuids.Count > 0)
            {
                if (!selectedPuuids.Contains(friendPuuid, StringComparer.OrdinalIgnoreCase))
                {
                    _log.Debug(LogSource, $"Skipping party join - friend {friendData.GameName} is not in selected list");
                    return;
                }
            }

            // Check if we're in a state where joining is allowed
            if (CurrentState != GameState.MainMenu)
            {
                _log.Debug(LogSource, $"Skipping party join - current state is {CurrentState}");
                return;
            }

            // Prevent duplicate join attempts to the same party
            var partyId = partyInfo.PartyId;
            if (_recentPartyJoins.Contains(partyId))
            {
                _log.Debug(LogSource, $"Skipping party join - already attempted to join {partyId} recently");
                return;
            }

            // Prevent rapid successive join attempts (debounce 5 seconds)
            if ((DateTime.Now - _lastPartyJoinTime).TotalSeconds < 5)
            {
                _log.Debug(LogSource, "Skipping party join - too many join attempts");
                return;
            }

            // Join the party
            _log.Info(LogSource, $"Auto-joining friend {friendData.GameName}'s party {partyId}");
            _lastPartyJoinTime = DateTime.Now;
            _recentPartyJoins.Add(partyId);
            
            // Clean up old entries after 30 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(30000).ConfigureAwait(false);
                _recentPartyJoins.Remove(partyId);
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    // Check if we're already in a party before joining
                    if (await _lcuApiService.IsInPartyAsync().ConfigureAwait(false))
                    {
                        _log.Debug(LogSource, "Skipping party join - already in a party");
                        return;
                    }
                    
                    await _lcuApiService.JoinPartyAsync(partyId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warning(LogSource, $"Failed to join party: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _log.Warning(LogSource, $"Failed to process friend info event: {ex.Message}");
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
                    //await Task.Delay(500).ConfigureAwait(false);
                    await _lcuApiService.PlayAgainAsync().ConfigureAwait(false);
                    _log.Info(LogSource, "Play again queued");
                }
                catch (Exception ex) { _log.Warning(LogSource, $"Play again failed: {ex.Message}"); }
            });
        }

        if (nextState == GameState.Reconnect && _settings.Current.AutoReconnect)
        {
            _log.Info(LogSource, "Auto-reconnecting to game...");
            _ = _lcuApiService.ReconnectAsync().ConfigureAwait(false);
        }

        // Auto-send message on Champ Select
        if (nextState == GameState.ChampSelect && _settings.Current.AutoSendChampSelectMessage)
        {
            var message = _settings.Current.ChampSelectMessage;
            if (!string.IsNullOrWhiteSpace(message))
            {
                _log.Info(LogSource, "Champ Select entered, starting auto-send...");
                _ = Task.Run(async () => await SendAutoMessageAsync("champSelect", message));
            }
        }

        // Auto-send message on End Of Game
        if (nextState == GameState.EndOfGame && _settings.Current.AutoSendEndOfGameMessage)
        {
            var message = _settings.Current.EndOfGameMessage;
            if (!string.IsNullOrWhiteSpace(message))
            {
                _log.Info(LogSource, "End Of Game entered, starting auto-send...");
                _ = Task.Run(async () => await SendAutoMessageAsync("postGame", message));
            }
        }

        // Auto-send message on Lobby
        if (nextState == GameState.Lobby && _settings.Current.AutoSendLobbyMessage)
        {
            var message = _settings.Current.LobbyMessage;
            if (!string.IsNullOrWhiteSpace(message))
            {
                _log.Info(LogSource, "Lobby entered, starting auto-send...");
                _ = Task.Run(async () => await SendLobbyAutoMessageAsync(message));
            }
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

    private async Task SendAutoMessageAsync(string conversationType, string message)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            var session = await _lcuApiService.GetGameFlowSessionAsync(cts.Token).ConfigureAwait(false);
            if (session == null)
            {
                _log.Warning(LogSource, "Failed to get game session");
                return;
            }

            var playerPuuids = session.GameData.TeamPlayers
                .Select(p => p.Puuid)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (playerPuuids.Count == 0)
            {
                _log.Warning(LogSource, "No players found in game session");
                return;
            }

            // Poll for conversation and check participants
            for (int i = 0; i < 10; i++)
            {
                if (cts.Token.IsCancellationRequested)
                    break;

                var conversations = await _lcuApiService.GetConversationsAsync(cts.Token).ConfigureAwait(false);
                var targetConversation = conversations.FirstOrDefault(c => c.Type == conversationType);

                if (targetConversation == null)
                {
                    _log.Debug(LogSource, $"Conversation {conversationType} not found, retry {i + 1}/10");
                    await Task.Delay(500, cts.Token).ConfigureAwait(false);
                    continue;
                }

                var allPlayersPresent = playerPuuids.All(puuid => 
                    targetConversation.Participants.Contains(puuid, StringComparer.OrdinalIgnoreCase));
                if (allPlayersPresent)
                {
                    await _lcuApiService.SendChatMessageAsync(targetConversation.Id, message, cts.Token).ConfigureAwait(false);
                    _log.Info(LogSource, $"Auto-sent message to {conversationType}");
                    return;
                }
                else
                {
                    _log.Debug(LogSource, $"Not all players in chat, retry {i + 1}/10");
                    await Task.Delay(500, cts.Token).ConfigureAwait(false);
                }
            }

            _log.Warning(LogSource, $"Timeout waiting for players to join {conversationType} chat");
        }
        catch (OperationCanceledException)
        {
            _log.Warning(LogSource, $"Timeout waiting for players to join {conversationType} chat");
        }
        catch (Exception ex)
        {
            _log.Warning(LogSource, $"Auto-send failed: {ex.Message}");
        }
    }

    private async Task SendLobbyAutoMessageAsync(string message)
    {
        try
        {
            // Small delay to allow lobby chat to initialize
            await Task.Delay(500).ConfigureAwait(false);

            var conversations = await _lcuApiService.GetConversationsAsync(CancellationToken.None).ConfigureAwait(false);
            var lobbyConv = conversations.FirstOrDefault(c => c.Type == "lobby");
            
            if (lobbyConv != null)
            {
                await _lcuApiService.SendChatMessageAsync(lobbyConv.Id, message, CancellationToken.None).ConfigureAwait(false);
                _log.Info(LogSource, "Auto-sent message to lobby");
            }
            else
            {
                _log.Warning(LogSource, "Lobby conversation not found");
            }
        }
        catch (Exception ex)
        {
            _log.Warning(LogSource, $"Lobby auto-send failed: {ex.Message}");
        }
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
