using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class GameStatusViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILcuApiService _api;
    private readonly ILoggingService _log;
    private readonly ILocalizationService _localization;
    private const string LogSource = "GameStatusViewModel";

    [ObservableProperty] private string queueType = "RANKED_SOLO_5x5";
    [ObservableProperty] private string tier = "CHALLENGER";
    [ObservableProperty] private string division = "I";
    [ObservableProperty] private string status = "chat";

    public string GameModeLabel => _localization.Get("game_status.game_mode");
    public string RankingLabel => _localization.Get("game_status.ranking");
    public string LevelLabel => _localization.Get("game_status.level");
    public string StatusLabel => _localization.Get("game_status.status");
    public string ApplyButtonLabel => _localization.Get("game_status.apply");

    public ObservableCollection<string> QueueTypes { get; } = ["RANKED_SOLO_5x5", "RANKED_FLEX_SR", "RANKED_FLEX_TT", "RANKED_TFT"];
    public ObservableCollection<string> Tiers { get; } = ["IRON", "BRONZE", "SILVER", "GOLD", "PLATINUM", "DIAMOND", "MASTER", "GRANDMASTER", "CHALLENGER"];
    public ObservableCollection<string> Divisions { get; } = ["IV", "III", "II", "I"];
    public ObservableCollection<string> StatusOptions { get; } = ["chat", "away", "offline", "mobile"];

    public GameStatusViewModel(ISettingsService settings, ILcuApiService api, ILoggingService log, ILocalizationService localization)
    {
        _settings = settings;
        _api = api;
        _log = log;
        _localization = localization;
        LoadSettings();
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    private void LoadSettings()
    {
        var s = _settings.Current;
        QueueType = s.QueueType;
        Tier = s.Tier;
        Division = s.Division;
        Status = s.Status;
    }

    partial void OnQueueTypeChanged(string value) => Save(s => s.QueueType = value);
    partial void OnTierChanged(string value) => Save(s => s.Tier = value);
    partial void OnDivisionChanged(string value) => Save(s => s.Division = value);
    partial void OnStatusChanged(string value) => Save(s => s.Status = value);

    private void Save(Action<AppSettings> mutate)
    {
        var current = _settings.Current;
        mutate(current);
        _settings.Save(current);
    }

    [RelayCommand]
    private async Task ApplyFakeRankAsync()
    {
        if (!_api.IsConfigured)
            return;

        await _api.UpdateChatMeAsync(QueueType, Tier, Division, Status).ConfigureAwait(false);
        _log.Info(LogSource, "Fake rank/status updated.");
    }
}