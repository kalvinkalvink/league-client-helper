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
    private const string LogSource = "LobbyViewModel";

    [ObservableProperty] private string friendFilterGroup = "All";

    public ObservableCollection<string> FriendGroups { get; } = ["All"];

    public LobbyViewModel(ISettingsService settings, ILcuApiService api, ILoggingService log)
    {
        _settings = settings;
        _api = api;
        _log = log;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var s = _settings.Current;
        FriendFilterGroup = s.FriendFilterGroup;
    }

    partial void OnFriendFilterGroupChanged(string value) => Save(s => s.FriendFilterGroup = value);

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
}