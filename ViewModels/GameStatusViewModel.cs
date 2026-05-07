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

    [ObservableProperty] private LocalizedItem? selectedQueueTypeItem;
    [ObservableProperty] private LocalizedItem? selectedTierItem;
    [ObservableProperty] private LocalizedItem? selectedDivisionItem;
    [ObservableProperty] private LocalizedItem? selectedStatusItem;

    public string GameModeLabel => _localization.Get("game_status.game_mode");
    public string RankingLabel => _localization.Get("game_status.ranking");
    public string LevelLabel => _localization.Get("game_status.level");
    public string StatusLabel => _localization.Get("game_status.status");
    public string ApplyButtonLabel => _localization.Get("game_status.apply");

    public ObservableCollection<LocalizedItem> QueueTypeItems { get; } = [];
    public ObservableCollection<LocalizedItem> TierItems { get; } = [];
    public ObservableCollection<LocalizedItem> DivisionItems { get; } = [];
    public ObservableCollection<LocalizedItem> StatusItems { get; } = [];

    public GameStatusViewModel(ISettingsService settings, ILcuApiService api, ILoggingService log, ILocalizationService localization)
    {
        _settings = settings;
        _api = api;
        _log = log;
        _localization = localization;

        // Initialize collections with localized display text
        QueueTypeItems = new ObservableCollection<LocalizedItem>
        {
            new() { Display = _localization.Get("queue.solo"), Value = "RANKED_SOLO_5x5" },
            new() { Display = _localization.Get("queue.flex5"), Value = "RANKED_FLEX_SR" },
            new() { Display = _localization.Get("queue.flex3"), Value = "RANKED_FLEX_TT" },
            new() { Display = _localization.Get("queue.tft"), Value = "RANKED_TFT" }
        };
        TierItems = new ObservableCollection<LocalizedItem>
        {
            new() { Display = _localization.Get("tier.iron"), Value = "IRON" },
            new() { Display = _localization.Get("tier.bronze"), Value = "BRONZE" },
            new() { Display = _localization.Get("tier.silver"), Value = "SILVER" },
            new() { Display = _localization.Get("tier.gold"), Value = "GOLD" },
            new() { Display = _localization.Get("tier.platinum"), Value = "PLATINUM" },
            new() { Display = _localization.Get("tier.diamond"), Value = "DIAMOND" },
            new() { Display = _localization.Get("tier.master"), Value = "MASTER" },
            new() { Display = _localization.Get("tier.grandmaster"), Value = "GRANDMASTER" },
            new() { Display = _localization.Get("tier.challenger"), Value = "CHALLENGER" }
        };
        DivisionItems = new ObservableCollection<LocalizedItem>
        {
            new() { Display = _localization.Get("division.iv"), Value = "IV" },
            new() { Display = _localization.Get("division.iii"), Value = "III" },
            new() { Display = _localization.Get("division.ii"), Value = "II" },
            new() { Display = _localization.Get("division.i"), Value = "I" }
        };
        StatusItems = new ObservableCollection<LocalizedItem>
        {
            new() { Display = _localization.Get("status.online"), Value = "chat" },
            new() { Display = _localization.Get("status.away"), Value = "away" },
            new() { Display = _localization.Get("status.offline"), Value = "offline" },
            new() { Display = _localization.Get("status.mobile"), Value = "mobile" }
        };

        LoadSettings();
        _localization.LanguageChanged += (_, _) =>
        {
            // Update display text for all items
            QueueTypeItems[0].Display = _localization.Get("queue.solo");
            QueueTypeItems[1].Display = _localization.Get("queue.flex5");
            QueueTypeItems[2].Display = _localization.Get("queue.flex3");
            QueueTypeItems[3].Display = _localization.Get("queue.tft");
            TierItems[0].Display = _localization.Get("tier.iron");
            TierItems[1].Display = _localization.Get("tier.bronze");
            TierItems[2].Display = _localization.Get("tier.silver");
            TierItems[3].Display = _localization.Get("tier.gold");
            TierItems[4].Display = _localization.Get("tier.platinum");
            TierItems[5].Display = _localization.Get("tier.diamond");
            TierItems[6].Display = _localization.Get("tier.master");
            TierItems[7].Display = _localization.Get("tier.grandmaster");
            TierItems[8].Display = _localization.Get("tier.challenger");
            DivisionItems[0].Display = _localization.Get("division.iv");
            DivisionItems[1].Display = _localization.Get("division.iii");
            DivisionItems[2].Display = _localization.Get("division.ii");
            DivisionItems[3].Display = _localization.Get("division.i");
            StatusItems[0].Display = _localization.Get("status.online");
            StatusItems[1].Display = _localization.Get("status.away");
            StatusItems[2].Display = _localization.Get("status.offline");
            StatusItems[3].Display = _localization.Get("status.mobile");
            OnPropertyChanged(string.Empty);
        };
    }

    private void LoadSettings()
    {
        var s = _settings.Current;
        SelectedQueueTypeItem = QueueTypeItems.FirstOrDefault(x => x.Value == s.QueueType) ?? QueueTypeItems[0];
        SelectedTierItem = TierItems.FirstOrDefault(x => x.Value == s.Tier) ?? TierItems[0];
        SelectedDivisionItem = DivisionItems.FirstOrDefault(x => x.Value == s.Division) ?? DivisionItems[0];
        SelectedStatusItem = StatusItems.FirstOrDefault(x => x.Value == s.Status) ?? StatusItems[0];
    }

    partial void OnSelectedQueueTypeItemChanged(LocalizedItem? value)
    {
        if (value is null) return;
        _log.Debug(LogSource, $"QueueType changed to {value.Value}");
        Save(s => s.QueueType = value.Value);
    }

    partial void OnSelectedTierItemChanged(LocalizedItem? value)
    {
        if (value is null) return;
        _log.Debug(LogSource, $"Tier changed to {value.Value}");
        Save(s => s.Tier = value.Value);
    }

    partial void OnSelectedDivisionItemChanged(LocalizedItem? value)
    {
        if (value is null) return;
        _log.Debug(LogSource, $"Division changed to {value.Value}");
        Save(s => s.Division = value.Value);
    }

    partial void OnSelectedStatusItemChanged(LocalizedItem? value)
    {
        if (value is null) return;
        _log.Debug(LogSource, $"Status changed to {value.Value}");
        Save(s => s.Status = value.Value);
    }

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

        await _api.UpdateChatMeAsync(SelectedQueueTypeItem?.Value ?? "RANKED_SOLO_5x5",
            SelectedTierItem?.Value ?? "CHALLENGER",
            SelectedDivisionItem?.Value ?? "I",
            SelectedStatusItem?.Value ?? "chat").ConfigureAwait(false);
        _log.Info(LogSource, "Fake rank/status updated.");
    }
}