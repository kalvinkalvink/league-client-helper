using CommunityToolkit.Mvvm.ComponentModel;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class ChampSelectViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILoggingService _log;
    private readonly ILocalizationService _localization;
    private const string LogSource = "ChampSelectViewModel";

    [ObservableProperty] private bool autoSendChampSelectMessage;
    [ObservableProperty] private string champSelectMessage = string.Empty;

    public string ChampSelectTabText => _localization.Get("tab.champ_select");
    public string AutoSendLabel => _localization.Get("champ_select.auto_send");
    public string MessagePlaceholder => _localization.Get("champ_select.message_placeholder");

    public ChampSelectViewModel(ISettingsService settings, ILoggingService log, ILocalizationService localization)
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
        AutoSendChampSelectMessage = s.AutoSendChampSelectMessage;
        ChampSelectMessage = s.ChampSelectMessage;
    }

    partial void OnAutoSendChampSelectMessageChanged(bool value)
    {
        _log.Debug(LogSource, $"AutoSendChampSelectMessage changed to {value}");
        Save(s => s.AutoSendChampSelectMessage = value);
    }
    partial void OnChampSelectMessageChanged(string value)
    {
        _log.Debug(LogSource, $"ChampSelectMessage changed to '{value}'");
        Save(s => s.ChampSelectMessage = value);
    }

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }
}
