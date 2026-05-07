using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ILoggingService _log;
    private readonly ILocalizationService _localization;
    [ObservableProperty] private int maxLogEntries = 1000;
    [ObservableProperty] private string maxLogEntriesText = "1000";

    [ObservableProperty] private string searchText = string.Empty;

    public string LogsLabel             => _localization.Get("tab.logs");
    public string ClearLogsLabel        => _localization.Get("logs.clear");
    public string OpenLogsFolderLabel   => _localization.Get("logs.open_folder");
    public string SearchLogsPlaceholder => _localization.Get("logs.search_placeholder");

    /// <summary>All log entries, capped at MaxLogEntries (newest at end).</summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    /// <summary>
    /// Filtered view of LogEntries for the CollectionView.
    /// Rebuilt when SearchText changes or a new entry arrives.
    /// Backed by an ObservableCollection so the UI only re-renders changed rows,
    /// replacing the old single-Editor AutoSize approach that thrashed on every append.
    /// </summary>
    public ObservableCollection<LogEntry> FilteredLogs { get; } = [];

    public LogsViewModel(ILoggingService log, ILocalizationService localization)
    {
        _log = log;
        _localization = localization;
        _log.LogEntryWritten += OnLogEntryWritten;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    private void OnLogEntryWritten(object? sender, LogEntry entry)
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            // Enforce cap before adding to avoid unbounded growth
            while (LogEntries.Count >= MaxLogEntries)
                LogEntries.RemoveAt(0);

            LogEntries.Add(entry);

            // Only add to FilteredLogs when it passes the current search filter,
            // so we avoid rebuilding the entire list on every new entry.
            if (MatchesSearch(entry))
            {
                while (FilteredLogs.Count >= MaxLogEntries)
                    FilteredLogs.RemoveAt(0);
                FilteredLogs.Add(entry);
            }
        });
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogEntries.Clear();
        FilteredLogs.Clear();
    }

    [RelayCommand]
    private async Task OpenLogsFolderAsync()
    {
        var logDir = LoggingService.LogDirectory;
        if (Directory.Exists(logDir))
            await Launcher.OpenAsync(new Uri(logDir));
    }

        partial void OnSearchTextChanged(string value) => RebuildFilteredLogs();

        partial void OnMaxLogEntriesChanged(int oldValue, int newValue)
        {
            MaxLogEntriesText = newValue.ToString();
            TrimLogEntries();
        }

        private void RebuildFilteredLogs()
        {
            FilteredLogs.Clear();
            foreach (var entry in LogEntries.Where(MatchesSearch))
                FilteredLogs.Add(entry);
        }

        private void TrimLogEntries()
        {
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                while (LogEntries.Count > MaxLogEntries)
                    LogEntries.RemoveAt(0);
                while (FilteredLogs.Count > MaxLogEntries)
                    FilteredLogs.RemoveAt(0);
            });
        }

        private bool MatchesSearch(LogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var needle = SearchText.ToLowerInvariant();
        return entry.Level.ToLowerInvariant().Contains(needle)
            || entry.Source.ToLowerInvariant().Contains(needle)
            || entry.Message.ToLowerInvariant().Contains(needle);
    }
}
