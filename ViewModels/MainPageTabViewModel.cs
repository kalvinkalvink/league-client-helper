using CommunityToolkit.Mvvm.ComponentModel;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class MainPageTabViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private bool autoAcceptInvite;

    public MainPageTabViewModel(ISettingsService settings)
    {
        _settings = settings;
        LoadSettings();
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