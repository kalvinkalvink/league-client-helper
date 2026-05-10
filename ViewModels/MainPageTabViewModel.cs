using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class MainPageTabViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly ILocalizationService _localization;
    private readonly ILcuApiService _api;
    private readonly ILoggingService _log;
    private readonly IGameStateService _gameState;
    private const string LogSource = "MainPageTabViewModel";
    private bool _disposed;

    [ObservableProperty] private bool autoAcceptInvite;
    [ObservableProperty] private bool autoJoinFriendParty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private Friend? selectedFriendInAllList;
    [ObservableProperty] private Friend? selectedFriendInSelectedList;
    [ObservableProperty] private bool isRefreshingFriends;

    public ObservableCollection<Friend> AllFriends { get; } = new();
    public ObservableCollection<Friend> FilteredFriends { get; } = new();
    public ObservableCollection<Friend> SelectedFriends { get; } = new();

    public string MainPageLabel => _localization.Get("tab.main_page");
    public string AutoAcceptInviteLabel => _localization.Get("main_page.auto_accept_invite");
    public string AutoJoinFriendPartyLabel => _localization.Get("main_page.auto_join_friend_party");
    public string FriendsLabel => _localization.Get("main_page.friends");
    public string SelectedFriendsLabel => _localization.Get("main_page.selected_friends");
    public string SearchFriendsPlaceholder => _localization.Get("main_page.search_friends");
    public string RefreshFriendsLabel => _localization.Get("main_page.refresh_friends");
    public string AddFriendLabel => _localization.Get("main_page.add");
    public string RemoveFriendLabel => _localization.Get("main_page.remove");

    public MainPageTabViewModel(ISettingsService settings, ILocalizationService localization, ILcuApiService api, ILoggingService log, IGameStateService gameState)
    {
        _settings = settings;
        _localization = localization;
        _api = api;
        _log = log;
        _gameState = gameState;
        LoadSettings();
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
        _gameState.FriendGameStatusChanged += OnFriendGameStatusChanged;
    }

    private void OnFriendGameStatusChanged(string puuid, string gameStatus, string product)
    {
        _log.Debug(LogSource, $"RECEIVED: puuid={puuid}, gameStatus={gameStatus}, product={product}");
        
        // Update in AllFriends
        var friend = AllFriends.FirstOrDefault(f => f.Puuid == puuid);
        if (friend != null)
        {
            _log.Debug(LogSource, $"Updating AllFriends: {friend.Name} GameStatus: {friend.GameStatus} -> {gameStatus}");
            friend.GameStatus = gameStatus;
            friend.Product = product;
        }
        else
        {
            _log.Warning(LogSource, $"Friend {puuid} NOT FOUND in AllFriends");
        }

        // Update in SelectedFriends
        friend = SelectedFriends.FirstOrDefault(f => f.Puuid == puuid);
        if (friend != null)
        {
            _log.Debug(LogSource, $"Updating SelectedFriends: {friend.Name} GameStatus: {friend.GameStatus} -> {gameStatus}");
            friend.GameStatus = gameStatus;
            friend.Product = product;
        }

        // Re-filter to update FilteredFriends display
        FilterFriends();
    }

    private void LoadSettings()
    {
        var s = _settings.Current;
        AutoAcceptInvite = s.AutoAcceptInvite;
        AutoJoinFriendParty = s.AutoJoinFriendParty;
        
        // Restore selected friends from settings
        var selectedPuuids = s.SelectedFriendPuuids ?? new List<string>();
        // We'll populate SelectedFriends after loading friends
    }

    partial void OnAutoAcceptInviteChanged(bool value)
    {
        _log.Debug(LogSource, $"AutoAcceptInvite changed to {value}");
        Save(s => s.AutoAcceptInvite = value);
    }
    partial void OnAutoJoinFriendPartyChanged(bool value)
    {
        _log.Debug(LogSource, $"AutoJoinFriendParty changed to {value}");
        Save(s => s.AutoJoinFriendParty = value);
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterFriends();
    }

    private void FilterFriends()
    {
        FilteredFriends.Clear();
        var searchLower = SearchText?.ToLowerInvariant() ?? string.Empty;
        
        var filtered = AllFriends
            .Where(f => string.IsNullOrEmpty(searchLower) || 
                        f.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                        f.GroupName.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => GetOnlinePriority(f.EffectiveAvailability))
            .ThenBy(f => f.Name);
        
        foreach (var friend in filtered)
            FilteredFriends.Add(friend);

        _log.Debug(LogSource, $"Friend list filtered, visible count: {FilteredFriends.Count}");
    }

    private static bool IsOnline(string availability)
    {
        return !string.Equals(availability, "offline", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(availability, "mobile", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetOnlinePriority(string effectiveAvailability)
    {
        return effectiveAvailability.ToLowerInvariant() switch
        {
            "online" => 0,       // chat + league_of_legends
            "chat" => 1,         // chat + other product
            "inGame" => 2,       // in game
            "hosting" => 3,       // hosting a game
            "away" => 4,          // away status
            "mobile" => 5,        // mobile
            "offline" => 6,       // offline
            _ => 7
        };
    }

    [RelayCommand]
    private async Task RefreshFriendsAsync()
    {
        if (!_api.IsConfigured || IsRefreshingFriends)
            return;

        IsRefreshingFriends = true;
        try
        {
            var friends = await _api.GetFriendsAsync().ConfigureAwait(false);
            var selectedPuuids = _settings.Current.SelectedFriendPuuids ?? new List<string>();
            
            // Clear and repopulate
            AllFriends.Clear();
            SelectedFriends.Clear();

            foreach (var friend in friends.OrderBy(f => GetOnlinePriority(f.EffectiveAvailability)).ThenBy(f => f.Name))
            {
                AllFriends.Add(friend);
                if (selectedPuuids.Contains(friend.Puuid))
                    SelectedFriends.Add(friend);
            }

            _log.Debug(LogSource, $"Friend list refreshed, total count: {AllFriends.Count}, selected count: {SelectedFriends.Count}");
            FilterFriends();
        }
        catch (Exception ex)
        {
            _log.Error(LogSource, "Failed to refresh friends", ex);
        }
        finally
        {
            IsRefreshingFriends = false;
        }
    }

    [RelayCommand]
    private void AddFriend()
    {
        if (SelectedFriendInAllList is null)
            return;

        var friend = SelectedFriendInAllList;
        
        // Remove from AllFriends (will be reflected in FilteredFriends via re-filter)
        AllFriends.Remove(friend);
        SelectedFriends.Add(friend);
        
        FilterFriends();
        SaveSelectedFriends();
        
        SelectedFriendInAllList = null;
    }

    [RelayCommand]
    private void RemoveFriend()
    {
        if (SelectedFriendInSelectedList is null)
            return;

        var friend = SelectedFriendInSelectedList;
        
        // Add back to AllFriends with proper sorting
        AllFriends.Add(friend);
        SelectedFriends.Remove(friend);
        
        // Re-sort AllFriends using EffectiveAvailability (same as the add/refresh paths)
        // Bug fix: was using raw f.Availability instead of f.EffectiveAvailability, and
        // OrderByDescending instead of OrderBy (higher priority = lower int value).
        var sorted = AllFriends.OrderBy(f => GetOnlinePriority(f.EffectiveAvailability)).ThenBy(f => f.Name).ToList();
        AllFriends.Clear();
        foreach (var f in sorted) AllFriends.Add(f);
        
        FilterFriends();
        SaveSelectedFriends();
        
        SelectedFriendInSelectedList = null;
    }

    private void SaveSelectedFriends()
    {
        Save(s => s.SelectedFriendPuuids = SelectedFriends.Select(f => f.Puuid).ToList());
    }

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _localization.LanguageChanged -= (_, _) => OnPropertyChanged(string.Empty);
        _gameState.FriendGameStatusChanged -= OnFriendGameStatusChanged;
    }
}