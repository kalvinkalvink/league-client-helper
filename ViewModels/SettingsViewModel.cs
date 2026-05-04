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
    [ObservableProperty] private LocalizedItem? selectedLanguageItem;
    [ObservableProperty] private LocalizedItem? selectedThemeItem;
    [ObservableProperty] private bool changeRankingOnStart;
    [ObservableProperty] private bool autoReconnect;

    public ObservableCollection<int> PollIntervals { get; } = [250, 500, 1000, 2000];
    public ObservableCollection<LocalizedItem> LanguageItems { get; } = [];
    public ObservableCollection<LocalizedItem> ThemeItems { get; } = [];

    public SettingsViewModel(ISettingsService settings, ILocalizationService localization)
    {
        _settings = settings;
        _localization = localization;

        // Initialize collections with localized display text
        LanguageItems = new ObservableCollection<LocalizedItem>
        {
            new() { Display = _localization.Get("language.en"), Value = "en" },
            new() { Display = _localization.Get("language.zh-CN"), Value = "zh-CN" },
            new() { Display = _localization.Get("language.zh-TW"), Value = "zh-TW" }
        };
        ThemeItems = new ObservableCollection<LocalizedItem>
        {
            new() { Display = _localization.Get("theme.auto"), Value = "Auto" },
            new() { Display = _localization.Get("theme.light"), Value = "Light" },
            new() { Display = _localization.Get("theme.dark"), Value = "Dark" }
        };

        var s = _settings.Current;
        PollIntervalMs = s.PollIntervalMs;
        ChangeRankingOnStart = s.ChangeRankingOnStart;
        AutoReconnect = s.AutoReconnect;

        // Set selected items based on saved values
        SelectedLanguageItem = LanguageItems.FirstOrDefault(x => x.Value == s.Language) ?? LanguageItems[0];
        SelectedThemeItem = ThemeItems.FirstOrDefault(x => x.Value == s.Theme) ?? ThemeItems[0];

        _localization.LanguageChanged += (_, _) =>
        {
            // Update display text for all items
            LanguageItems[0].Display = _localization.Get("language.en");
            LanguageItems[1].Display = _localization.Get("language.zh-CN");
            LanguageItems[2].Display = _localization.Get("language.zh-TW");
            ThemeItems[0].Display = _localization.Get("theme.auto");
            ThemeItems[1].Display = _localization.Get("theme.light");
            ThemeItems[2].Display = _localization.Get("theme.dark");
            OnPropertyChanged(string.Empty);
        };
    }

    public string PollIntervalLabel => _localization.Get("setting.poll_interval");
    public string LanguageLabel => _localization.Get("setting.language");
    public string ThemeLabel => _localization.Get("setting.theme");
    public string ChangeRankingOnStartLabel => _localization.Get("setting.change_ranking_on_start");
    public string AutoReconnectLabel => _localization.Get("setting.auto_reconnect");
    public string ResetButtonLabel => _localization.Get("setting.reset");

    partial void OnPollIntervalMsChanged(int value) => Save(s => s.PollIntervalMs = value);

    partial void OnSelectedLanguageItemChanged(LocalizedItem? value)
    {
        if (value is null) return;
        Save(s => s.Language = value.Value);
        _localization.SetLanguage(value.Value);
    }

    partial void OnSelectedThemeItemChanged(LocalizedItem? value)
    {
        if (value is null) return;
        Save(s => s.Theme = value.Value);
        
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = value.Value switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }

    partial void OnChangeRankingOnStartChanged(bool value) => Save(s => s.ChangeRankingOnStart = value);
    partial void OnAutoReconnectChanged(bool value) => Save(s => s.AutoReconnect = value);

    [RelayCommand]
    private void ResetToDefaults()
    {
        var defaults = new AppSettings();
        PollIntervalMs = defaults.PollIntervalMs;
        ChangeRankingOnStart = defaults.ChangeRankingOnStart;
        AutoReconnect = defaults.AutoReconnect;
        SelectedLanguageItem = LanguageItems.FirstOrDefault(x => x.Value == defaults.Language) ?? LanguageItems[0];
        SelectedThemeItem = ThemeItems.FirstOrDefault(x => x.Value == defaults.Theme) ?? ThemeItems[0];
        _settings.Save(defaults);
    }

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }
}
