using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGameStateService _gameStateService;
    private readonly ILoggingService _log;

    [ObservableProperty] private string selectedTab = "GameAuto";
    [ObservableProperty] private string connectionStatus = "Connecting...";

    public GameAutoViewModel GameAutoVM { get; }
    public MainPageTabViewModel MainPageTabVM { get; }
    public LobbyViewModel LobbyVM { get; }
    public GameStatusViewModel GameStatusVM { get; }
    public LogsViewModel LogsVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public bool IsGameAutoTab => SelectedTab == "GameAuto";
    public bool IsMainPageTab => SelectedTab == "MainPage";
    public bool IsLobbyTab => SelectedTab == "Lobby";
    public bool IsGameStatusTab => SelectedTab == "GameStatus";
    public bool IsLogsTab => SelectedTab == "Logs";
    public bool IsSettingsTab => SelectedTab == "Settings";

    public MainViewModel(
        GameAutoViewModel gameAutoVM,
        MainPageTabViewModel mainPageTabVM,
        LobbyViewModel lobbyVM,
        GameStatusViewModel gameStatusVM,
        LogsViewModel logsVM,
        SettingsViewModel settingsVM,
        IGameStateService gameStateService,
        ILoggingService log)
    {
        GameAutoVM = gameAutoVM;
        MainPageTabVM = mainPageTabVM;
        LobbyVM = lobbyVM;
        GameStatusVM = gameStatusVM;
        LogsVM = logsVM;
        SettingsVM = settingsVM;
        _gameStateService = gameStateService;
        _log = log;

        _gameStateService.GameStateChanged += OnGameStateChanged;
        _gameStateService.ApiConfigured += OnApiConfigured;
    }

    private async void OnApiConfigured(object? sender, EventArgs e)
    {
        await LobbyVM.RefreshFriendsCommand.ExecuteAsync(null);
    }

    partial void OnSelectedTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsGameAutoTab));
        OnPropertyChanged(nameof(IsMainPageTab));
        OnPropertyChanged(nameof(IsLobbyTab));
        OnPropertyChanged(nameof(IsGameStatusTab));
        OnPropertyChanged(nameof(IsLogsTab));
        OnPropertyChanged(nameof(IsSettingsTab));
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