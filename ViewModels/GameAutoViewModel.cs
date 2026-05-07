using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class GameAutoViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILoggingService _log;
    private readonly ILocalizationService _localization;
    private const string LogSource = "GameAutoViewModel";

    [ObservableProperty] private bool autoStartGame;
    [ObservableProperty] private bool autoAcceptMatch;
    [ObservableProperty] private bool autoSkipLike;
    [ObservableProperty] private bool autoReenterLobby;

    public string AutoStartGameLabel => _localization.Get("game_auto.start_game");
    public string AutoAcceptMatchLabel => _localization.Get("game_auto.accept_match");
    public string AutoSkipLikeLabel => _localization.Get("game_auto.skip_like");
    public string AutoReenterLobbyLabel => _localization.Get("game_auto.reenter_lobby");

    public GameAutoViewModel(ISettingsService settings, ILoggingService log, ILocalizationService localization)
    {
        _settings = settings;
        _log = log;
        _localization = localization;
        LoadSettings();
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    private void LoadSettings()
    {
        var s = _settings.Current;
        AutoStartGame = s.AutoStartGame;
        AutoAcceptMatch = s.AutoAcceptMatch;
        AutoSkipLike = s.AutoSkipLike;
        AutoReenterLobby = s.AutoReenterLobby;
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
    partial void OnAutoSkipLikeChanged(bool value)
    {
        _log.Debug(LogSource, $"AutoSkipLike changed to {value}");
        Save(s => s.AutoSkipLike = value);
    }
    partial void OnAutoReenterLobbyChanged(bool value)
    {
        _log.Debug(LogSource, $"AutoReenterLobby changed to {value}");
        Save(s => s.AutoReenterLobby = value);
    }

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }
}