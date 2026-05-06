using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGameStateService _gameStateService;
    private readonly ILoggingService _log;
    private readonly ILocalizationService _localization;

    [ObservableProperty] private string selectedTab = "GameAuto";
    [ObservableProperty] private string connectionStatus = "Connecting...";

    public GameAutoViewModel GameAutoVM { get; }
    public MainPageTabViewModel MainPageTabVM { get; }
    public LobbyViewModel LobbyVM { get; }
    public GameStatusViewModel GameStatusVM { get; }
    public LogsViewModel LogsVM { get; }
    public SettingsViewModel SettingsVM { get; }
    public ChampSelectViewModel ChampSelectVM { get; }
    public EndOfGameViewModel EndOfGameVM { get; }

    public bool IsGameAutoTab => SelectedTab == "GameAuto";
    public bool IsMainPageTab => SelectedTab == "MainPage";
    public bool IsLobbyTab => SelectedTab == "Lobby";
    public bool IsGameStatusTab => SelectedTab == "GameStatus";
    public bool IsLogsTab => SelectedTab == "Logs";
    public bool IsSettingsTab => SelectedTab == "Settings";
    public bool IsChampSelectTab => SelectedTab == "ChampSelect";
    public bool IsEndOfGameTab => SelectedTab == "EndOfGame";

    public string GameAutoTabText => _localization.Get("tab.game_auto");
    public string MainPageTabText => _localization.Get("tab.main_page");
    public string LobbyTabText => _localization.Get("tab.lobby");
    public string GameStatusTabText => _localization.Get("tab.game_status");
    public string LogsTabText => _localization.Get("tab.logs");
    public string SettingsTabText => _localization.Get("menu.settings");
    public string ChampSelectTabText => _localization.Get("tab.champ_select");
    public string EndOfGameTabText => _localization.Get("tab.end_of_game");

    public MainViewModel(
        GameAutoViewModel gameAutoVM,
        MainPageTabViewModel mainPageTabVM,
        LobbyViewModel lobbyVM,
        GameStatusViewModel gameStatusVM,
        LogsViewModel logsVM,
        SettingsViewModel settingsVM,
        ChampSelectViewModel champSelectVM,
        EndOfGameViewModel endOfGameVM,
        IGameStateService gameStateService,
        ILoggingService log,
        ILocalizationService localization)
    {
        GameAutoVM = gameAutoVM;
        MainPageTabVM = mainPageTabVM;
        LobbyVM = lobbyVM;
        GameStatusVM = gameStatusVM;
        LogsVM = logsVM;
        SettingsVM = settingsVM;
        ChampSelectVM = champSelectVM;
        EndOfGameVM = endOfGameVM;
        _gameStateService = gameStateService;
        _log = log;
        _localization = localization;

        _gameStateService.GameStateChanged += OnGameStateChanged;
        _gameStateService.ApiConfigured += OnApiConfigured;
        _localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(GameAutoTabText));
            OnPropertyChanged(nameof(MainPageTabText));
            OnPropertyChanged(nameof(LobbyTabText));
            OnPropertyChanged(nameof(GameStatusTabText));
            OnPropertyChanged(nameof(LogsTabText));
            OnPropertyChanged(nameof(SettingsTabText));
            OnPropertyChanged(nameof(ChampSelectTabText));
            OnPropertyChanged(nameof(EndOfGameTabText));
        };
    }

    private void OnApiConfigured(object? sender, EventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher?.IsDispatchRequired == true)
        {
            dispatcher.Dispatch(() => LobbyVM.RefreshFriendsCommand.ExecuteAsync(null));
        }
        else
        {
            LobbyVM.RefreshFriendsCommand.ExecuteAsync(null);
        }
    }

    partial void OnSelectedTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsGameAutoTab));
        OnPropertyChanged(nameof(IsMainPageTab));
        OnPropertyChanged(nameof(IsLobbyTab));
        OnPropertyChanged(nameof(IsGameStatusTab));
        OnPropertyChanged(nameof(IsLogsTab));
        OnPropertyChanged(nameof(IsSettingsTab));
        OnPropertyChanged(nameof(IsChampSelectTab));
        OnPropertyChanged(nameof(IsEndOfGameTab));
    }

    [RelayCommand]
    private void SetTab(string tab) => SelectedTab = tab;

    [RelayCommand]
    private void New()
    {
        _log.Info("MainViewModel", "New command executed");
    }

    [RelayCommand]
    private void Open()
    {
        _log.Info("MainViewModel", "Open command executed");
    }

    [RelayCommand]
    private void Save()
    {
        _log.Info("MainViewModel", "Save command executed");
    }

    [RelayCommand]
    private static void Exit()
    {
        Application.Current.Quit();
    }

    private void OnGameStateChanged(object? sender, GameState state)
    {
        ConnectionStatus = state.ToString();
    }
}