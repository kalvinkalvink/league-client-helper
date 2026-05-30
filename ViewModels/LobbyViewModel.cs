using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class LobbyViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILcuApiService _api;
    private readonly ILoggingService _log;
    private readonly ILocalizationService _localization;
    private const string LogSource = "LobbyViewModel";

    [ObservableProperty] private LocalizedItem? selectedFriendGroupItem;
    [ObservableProperty] private bool autoSendLobbyMessage;
    [ObservableProperty] private string lobbyMessage = string.Empty;
    [ObservableProperty] private bool autoStartGame;
    [ObservableProperty] private bool autoAcceptMatch;

    public string LobbyLabel => _localization.Get("tab.lobby");
    public string InviteAllFriendsLabel => _localization.Get("lobby.invite_all");
    public string RefreshFriendsLabel => _localization.Get("lobby.refresh_friends");
    public string FilterLabel => _localization.Get("lobby.filter");
    public string AutoSendLabel => _localization.Get("lobby.auto_send");
    public string MessagePlaceholder => _localization.Get("lobby.message_placeholder");
    public string AutoStartGameLabel => _localization.Get("game_auto.start_game");
    public string AutoAcceptMatchLabel => _localization.Get("game_auto.accept_match");

    public ObservableCollection<LocalizedItem> FriendGroupItems { get; } = [
        new() { Display = "All", Value = "All" }
    ];

    public LobbyViewModel(ISettingsService settings, ILcuApiService api, ILoggingService log, ILocalizationService localization)
    {
        _settings = settings;
        _api = api;
        _log = log;
        _localization = localization;
        LoadSettings();
        _localization.LanguageChanged += (_, _) => {
            OnPropertyChanged(string.Empty);
            // Update "All" display text
            var allItem = FriendGroupItems.FirstOrDefault(x => x.Value == "All");
            if (allItem != null)
                allItem.Display = _localization.Get("filter.all");
            SelectedFriendGroupItem = FriendGroupItems.FirstOrDefault(x => x.Value == _settings.Current.FriendFilterGroup) ?? FriendGroupItems[0];
        };
    }

    private void LoadSettings()
    {
        var s = _settings.Current;
        SelectedFriendGroupItem = FriendGroupItems.FirstOrDefault(x => x.Value == s.FriendFilterGroup) ?? FriendGroupItems[0];
        AutoSendLobbyMessage = s.AutoSendLobbyMessage;
        LobbyMessage = s.LobbyMessage;
        AutoStartGame = s.AutoStartGame;
        AutoAcceptMatch = s.AutoAcceptMatch;
    }

    partial void OnSelectedFriendGroupItemChanged(LocalizedItem? value)
    {
        if (value is null) return;
        _log.Debug(LogSource, $"FriendFilterGroup changed to {value.Value}");
        Save(s => s.FriendFilterGroup = value.Value);
    }

    partial void OnAutoSendLobbyMessageChanged(bool value)
    {
        _log.Debug(LogSource, $"AutoSendLobbyMessage changed to {value}");
        Save(s => s.AutoSendLobbyMessage = value);
    }
    partial void OnAutoStartGameChanged(bool value)
    {
        _log.Debug(LogSource, $"AutoStartGame changed to {value}");
        Save(s => s.AutoStartGame = value);
    }
    partial void OnAutoAcceptMatchChanged(bool value)
    {
        _log.Debug(LogSource, $"AutoAcceptMatch changed to {value}");
        Save(s => s.AutoAcceptMatch = value);
    }
    partial void OnLobbyMessageChanged(string value)
    {
        _log.Debug(LogSource, $"LobbyMessage changed to '{value}'");
        Save(s => s.LobbyMessage = value);
    }

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }

    [RelayCommand]
    private async Task InviteAllFriendsAsync()
    {
        if (!_api.IsConfigured)
            return;

        var friends = await _api.GetFriendsAsync().ConfigureAwait(false);
        var filtered = friends
            .Where(f => !string.Equals(f.Availability, "offline", StringComparison.OrdinalIgnoreCase))
            .Where(f => SelectedFriendGroupItem?.Value == "All" || string.Equals(f.GroupName, SelectedFriendGroupItem?.Value, StringComparison.OrdinalIgnoreCase))
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
            // Keep the "All" item and add dynamic groups
            var allItem = FriendGroupItems[0]; // "All" item
            FriendGroupItems.Clear();
            allItem.Display = _localization.Get("filter.all");
            FriendGroupItems.Add(allItem);
            foreach (var group in groups)
                FriendGroupItems.Add(new LocalizedItem { Display = group, Value = group });

            SelectedFriendGroupItem = FriendGroupItems.FirstOrDefault(x => x.Value == _settings.Current.FriendFilterGroup) ?? FriendGroupItems[0];
        });
    }
}