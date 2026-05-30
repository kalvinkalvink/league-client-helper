using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IGameStateService _gameStateService;
    private readonly ILocalizationService _localization;
    private readonly ISettingsService _settings;
    private bool _disposed;

    [ObservableProperty] private string selectedTab = "Lobby";
    [ObservableProperty] private string connectionStatus = "Connecting...";

    public MainPageTabViewModel MainPageTabVM { get; }
    public LobbyViewModel LobbyVM { get; }
    public GameStatusViewModel GameStatusVM { get; }
    public LogsViewModel LogsVM { get; }
    public SettingsViewModel SettingsVM { get; }
    public ChampSelectViewModel ChampSelectVM { get; }
    public EndOfGameViewModel EndOfGameVM { get; }

    public bool IsMainPageTab    => SelectedTab == "MainPage";
    public bool IsLobbyTab       => SelectedTab == "Lobby";
    public bool IsGameStatusTab  => SelectedTab == "GameStatus";
    public bool IsLogsTab        => SelectedTab == "Logs";
    public bool IsSettingsTab    => SelectedTab == "Settings";
    public bool IsChampSelectTab => SelectedTab == "ChampSelect";
    public bool IsEndOfGameTab   => SelectedTab == "EndOfGame";

    public string MainPageTabText    => _localization.Get("tab.main_page");
    public string LobbyTabText       => _localization.Get("tab.lobby");
    public string GameStatusTabText  => _localization.Get("tab.game_status");
    public string LogsTabText        => _localization.Get("tab.logs");
    public string SettingsTabText    => _localization.Get("menu.settings");
    public string ChampSelectTabText => _localization.Get("tab.champ_select");
    public string EndOfGameTabText   => _localization.Get("tab.end_of_game");

    public MainViewModel(
        MainPageTabViewModel mainPageTabVM,
        LobbyViewModel lobbyVM,
        GameStatusViewModel gameStatusVM,
        LogsViewModel logsVM,
        SettingsViewModel settingsVM,
        ChampSelectViewModel champSelectVM,
        EndOfGameViewModel endOfGameVM,
        IGameStateService gameStateService,
        ILocalizationService localization,
        ISettingsService settings)
    {
        MainPageTabVM = mainPageTabVM;
        LobbyVM = lobbyVM;
        GameStatusVM = gameStatusVM;
        LogsVM = logsVM;
        SettingsVM = settingsVM;
        ChampSelectVM = champSelectVM;
        EndOfGameVM = endOfGameVM;
        _gameStateService = gameStateService;
        _localization = localization;
        _settings = settings;

        _gameStateService.GameStateChanged += OnGameStateChanged;
        _gameStateService.ApiConfigured += OnApiConfigured;

        // OnPropertyChanged(string.Empty) notifies all computed properties at once,
        // avoiding the need to enumerate every tab-text and IsXxxTab property by name.
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    private void OnApiConfigured(object? sender, EventArgs e)
    {
        // Always marshal to UI thread; Dispatcher.Dispatch is a no-op when already on UI.
        Application.Current?.Dispatcher.Dispatch(async () =>
        {
            // Refresh friends list
            await LobbyVM.RefreshFriendsCommand.ExecuteAsync(null);

            // Apply fake rank if setting is enabled
            if (_settings.Current.ChangeRankingOnStart)
            {
                await GameStatusVM.ApplyFakeRankCommand.ExecuteAsync(null);
            }
        });
    }

    partial void OnSelectedTabChanged(string value)
    {
        // Notify all IsXxxTab computed properties in one call.
        OnPropertyChanged(string.Empty);
    }

    [RelayCommand]
    private void SetTab(string tab) => SelectedTab = tab;

    [RelayCommand]
    private static void Exit() => Application.Current?.Quit();

    private void OnGameStateChanged(object? sender, GameState state)
    {
        ConnectionStatus = state.ToString();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _gameStateService.GameStateChanged -= OnGameStateChanged;
        _gameStateService.ApiConfigured -= OnApiConfigured;
        _localization.LanguageChanged -= (_, _) => OnPropertyChanged(string.Empty);
    }
}
