using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class GameAutoViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILoggingService _log;
    private const string LogSource = "GameAutoViewModel";

    [ObservableProperty] private bool autoStartGame;
    [ObservableProperty] private bool autoAcceptMatch;
    [ObservableProperty] private bool autoSkipLike;
    [ObservableProperty] private bool autoReenterLobby;

    public GameAutoViewModel(ISettingsService settings, ILoggingService log)
    {
        _settings = settings;
        _log = log;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var s = _settings.Current;
        AutoStartGame = s.AutoStartGame;
        AutoAcceptMatch = s.AutoAcceptMatch;
        AutoSkipLike = s.AutoSkipLike;
        AutoReenterLobby = s.AutoReenterLobby;
    }

    partial void OnAutoStartGameChanged(bool value) => Save(s => s.AutoStartGame = value);
    partial void OnAutoAcceptMatchChanged(bool value) => Save(s => s.AutoAcceptMatch = value);
    partial void OnAutoSkipLikeChanged(bool value) => Save(s => s.AutoSkipLike = value);
    partial void OnAutoReenterLobbyChanged(bool value) => Save(s => s.AutoReenterLobby = value);

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }
}