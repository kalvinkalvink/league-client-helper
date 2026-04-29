using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILocalizationService _localization;

    [ObservableProperty] private int pollIntervalMs;
    [ObservableProperty] private string language = "en";
    [ObservableProperty] private string theme = "Dark";
    [ObservableProperty] private bool changeRankingOnStart;

    public ObservableCollection<int> PollIntervals { get; } = [250, 500, 1000, 2000];
    public ObservableCollection<string> Themes { get; } = ["Auto", "Light", "Dark"];
    public ObservableCollection<string> Languages { get; } = ["en", "zh-CN", "zh-TW"];

    public SettingsViewModel(ISettingsService settings, ILocalizationService localization)
    {
        _settings = settings;
        _localization = localization;

        var s = _settings.Current;
        PollIntervalMs = s.PollIntervalMs;
        Language = s.Language;
        Theme = s.Theme;
        ChangeRankingOnStart = s.ChangeRankingOnStart;

        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    public string PollIntervalLabel => _localization.Get("setting.poll_interval");
    public string LanguageLabel => _localization.Get("setting.language");
    public string ThemeLabel => _localization.Get("setting.theme");
    public string ChangeRankingOnStartLabel => _localization.Get("setting.change_ranking_on_start");
    public string ResetButtonLabel => _localization.Get("setting.reset");

    partial void OnPollIntervalMsChanged(int value) => Save(s => s.PollIntervalMs = value);

    partial void OnLanguageChanged(string value)
    {
        Save(s => s.Language = value);
        _localization.SetLanguage(value);
    }

    partial void OnThemeChanged(string value)
    {
        Save(s => s.Theme = value);
        Application.Current!.UserAppTheme = value switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    partial void OnChangeRankingOnStartChanged(bool value) => Save(s => s.ChangeRankingOnStart = value);

    [RelayCommand]
    private void ResetToDefaults()
    {
        var defaults = new AppSettings();
        PollIntervalMs = defaults.PollIntervalMs;
        Language = defaults.Language;
        Theme = defaults.Theme;
        ChangeRankingOnStart = defaults.ChangeRankingOnStart;
        _settings.Save(defaults);
    }

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }
}
