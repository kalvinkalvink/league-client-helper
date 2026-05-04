using CommunityToolkit.Mvvm.ComponentModel;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class MainPageTabViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILocalizationService _localization;

    [ObservableProperty] private bool autoAcceptInvite;

    public string MainPageLabel => _localization.Get("tab.main_page");
    public string AutoAcceptInviteLabel => _localization.Get("main_page.auto_accept_invite");

    public MainPageTabViewModel(ISettingsService settings, ILocalizationService localization)
    {
        _settings = settings;
        _localization = localization;
        LoadSettings();
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    private void LoadSettings()
    {
        var s = _settings.Current;
        AutoAcceptInvite = s.AutoAcceptInvite;
    }

    partial void OnAutoAcceptInviteChanged(bool value) => Save(s => s.AutoAcceptInvite = value);

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }
}