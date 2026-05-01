using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string LogSource = "MainViewModel";
    private const int MaxLogEntries = 1000;

    private readonly ISettingsService _settings;
    private readonly ILcuApiService _api;
    private readonly IGameStateService _gameStateService;
    private readonly ILocalizationService _localization;
    private readonly IWindowService _windowService;
    private readonly ILoggingService _log;

    [ObservableProperty] private bool autoStartGame;
    [ObservableProperty] private bool autoAcceptMatch;
    [ObservableProperty] private bool autoSkipLike;
    [ObservableProperty] private bool autoReenterLobby;
    [ObservableProperty] private bool autoAcceptInvite;
    [ObservableProperty] private string friendFilterGroup = "All";
    [ObservableProperty] private string queueType = "RANKED_SOLO_5x5";
    [ObservableProperty] private string tier = "CHALLENGER";
    [ObservableProperty] private string division = "I";
    [ObservableProperty] private string status = "chat";
    [ObservableProperty] private string connectionStatus = "Connecting...";
    [ObservableProperty] private string selectedTab = "GameAuto";

    public ObservableCollection<string> FriendGroups { get; } = ["All"];
    public ObservableCollection<string> QueueTypes { get; } = ["RANKED_SOLO_5x5", "RANKED_FLEX_SR", "RANKED_FLEX_TT", "RANKED_TFT"];
    public ObservableCollection<string> Tiers { get; } = ["IRON", "BRONZE", "SILVER", "GOLD", "PLATINUM", "DIAMOND", "MASTER", "GRANDMASTER", "CHALLENGER"];
    public ObservableCollection<string> Divisions { get; } = ["IV", "III", "II", "I"];
    public ObservableCollection<string> StatusOptions { get; } = ["chat", "away", "dnd", "offline", "mobile"];
    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    public bool IsGameAutoTab => SelectedTab == "GameAuto";
    public bool IsMainPageTab => SelectedTab == "MainPage";
    public bool IsLobbyTab => SelectedTab == "Lobby";
    public bool IsGameStatusTab => SelectedTab == "GameStatus";
    public bool IsLogsTab => SelectedTab == "Logs";

    public MainViewModel(
        ISettingsService settings,
        ILcuApiService api,
        IGameStateService gameStateService,
        ILocalizationService localization,
        IWindowService windowService,
        ILoggingService log)
    {
        _settings = settings;
        _api = api;
        _gameStateService = gameStateService;
        _localization = localization;
        _windowService = windowService;
        _log = log;

        var s = _settings.Current;
        AutoStartGame = s.AutoStartGame;
        AutoAcceptMatch = s.AutoAcceptMatch;
        AutoSkipLike = s.AutoSkipLike;
        AutoReenterLobby = s.AutoReenterLobby;
        AutoAcceptInvite = s.AutoAcceptInvite;
        FriendFilterGroup = s.FriendFilterGroup;
        QueueType = s.QueueType;
        Tier = s.Tier;
        Division = s.Division;
        Status = s.Status;

        _gameStateService.GameStateChanged += OnGameStateChanged;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
        _log.LogEntryWritten += OnLogEntryWritten;
    }

    public string L(string key) => _localization.Get(key);

    partial void OnSelectedTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsGameAutoTab));
        OnPropertyChanged(nameof(IsMainPageTab));
        OnPropertyChanged(nameof(IsLobbyTab));
        OnPropertyChanged(nameof(IsGameStatusTab));
        OnPropertyChanged(nameof(IsLogsTab));
    }

    partial void OnAutoStartGameChanged(bool value) => Save(s => s.AutoStartGame = value);
    partial void OnAutoAcceptMatchChanged(bool value) => Save(s => s.AutoAcceptMatch = value);
    partial void OnAutoSkipLikeChanged(bool value) => Save(s => s.AutoSkipLike = value);
    partial void OnAutoReenterLobbyChanged(bool value) => Save(s => s.AutoReenterLobby = value);
    partial void OnAutoAcceptInviteChanged(bool value) => Save(s => s.AutoAcceptInvite = value);
    partial void OnFriendFilterGroupChanged(string value) => Save(s => s.FriendFilterGroup = value);
    partial void OnQueueTypeChanged(string value) => Save(s => s.QueueType = value);
    partial void OnTierChanged(string value) => Save(s => s.Tier = value);
    partial void OnDivisionChanged(string value) => Save(s => s.Division = value);
    partial void OnStatusChanged(string value) => Save(s => s.Status = value);

    [RelayCommand]
    private void OpenSettings() => _windowService.ShowSettingsWindow();

    [RelayCommand]
    private void SetTab(string tab) => SelectedTab = tab;

    [RelayCommand]
    private async Task ApplyFakeRankAsync()
    {
        if (!_api.IsConfigured)
            return;

        await _api.UpdateChatMeAsync(QueueType, Tier, Division, Status).ConfigureAwait(false);
        _log.Info(LogSource, "Fake rank/status updated.");
    }

    [RelayCommand]
    private async Task InviteAllFriendsAsync()
    {
        if (!_api.IsConfigured)
            return;

        var friends = await _api.GetFriendsAsync().ConfigureAwait(false);
        var filtered = friends
            .Where(f => !string.Equals(f.Availability, "offline", StringComparison.OrdinalIgnoreCase))
            .Where(f => FriendFilterGroup == "All" || string.Equals(f.GroupName, FriendFilterGroup, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.SummonerId)
            .Distinct()
            .ToArray();

        await _api.InviteFriendsAsync(filtered).ConfigureAwait(false);
        _log.Info(LogSource, $"Invited {filtered.Length} friends.");
    }

    [RelayCommand]
    private async Task RefreshFriendsAsync()
    {
        if (!_api.IsConfigured)
            return;

        var groups = (await _api.GetFriendsAsync().ConfigureAwait(false))
            .Select(f => string.IsNullOrWhiteSpace(f.GroupName) ? "Ungrouped" : f.GroupName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dispatcher = Application.Current?.Dispatcher;
        dispatcher?.Dispatch(() =>
        {
            FriendGroups.Clear();
            FriendGroups.Add("All");
            foreach (var group in groups)
                FriendGroups.Add(group);

            FriendFilterGroup = "All";
        });
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogEntries.Clear();
    }

    private void OnLogEntryWritten(object? sender, LogEntry entry)
    {
        var dispatcher = Application.Current?.Dispatcher;
        dispatcher?.Dispatch(() =>
        {
            LogEntries.Add(entry);
            while (LogEntries.Count > MaxLogEntries)
                LogEntries.RemoveAt(0);
        });
    }

    private void OnGameStateChanged(object? sender, GameState state)
    {
        ConnectionStatus = state.ToString();
    }

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }
}
