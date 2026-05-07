using CommunityToolkit.Mvvm.ComponentModel;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class EndOfGameViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILoggingService _log;
    private readonly ILocalizationService _localization;
    private const string LogSource = "EndOfGameViewModel";

    [ObservableProperty] private bool autoSendEndOfGameMessage;
    [ObservableProperty] private string endOfGameMessage = string.Empty;

    public string EndOfGameTabText => _localization.Get("tab.end_of_game");
    public string AutoSendLabel => _localization.Get("end_of_game.auto_send");
    public string MessagePlaceholder => _localization.Get("end_of_game.message_placeholder");

    public EndOfGameViewModel(ISettingsService settings, ILoggingService log, ILocalizationService localization)
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
        AutoSendEndOfGameMessage = s.AutoSendEndOfGameMessage;
        EndOfGameMessage = s.EndOfGameMessage;
    }

    partial void OnAutoSendEndOfGameMessageChanged(bool value)
    {
        _log.Debug(LogSource, $"AutoSendEndOfGameMessage changed to {value}");
        Save(s => s.AutoSendEndOfGameMessage = value);
    }
    partial void OnEndOfGameMessageChanged(string value)
    {
        _log.Debug(LogSource, $"EndOfGameMessage changed to '{value}'");
        Save(s => s.EndOfGameMessage = value);
    }

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }
}
