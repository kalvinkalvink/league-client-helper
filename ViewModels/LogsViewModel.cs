using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private const string LogSource = "LogsViewModel";
    private readonly ILoggingService _log;
    private readonly ILocalizationService _localization;
    private readonly ISettingsService _settings;
    [ObservableProperty] private int maxLogEntries = 1000;
    [ObservableProperty] private string maxLogEntriesText = "1000";

    [ObservableProperty] private string searchText = string.Empty;

    public string LogsLabel             => _localization.Get("tab.logs");
    public string ClearLogsLabel        => _localization.Get("logs.clear");
    public string OpenLogsFolderLabel   => _localization.Get("logs.open_folder");
    public string SearchLogsPlaceholder => _localization.Get("logs.search_placeholder");

    /// <summary>All log entries, capped at MaxLogEntries (newest at end).</summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    public LogsViewModel(ILoggingService log, ILocalizationService localization, ISettingsService settings)
    {
        _log = log;
        _localization = localization;
        _settings = settings;
        _log.LogEntryWritten += OnLogEntryWritten;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);

        // Initialize MaxLogEntries from settings
        var s = _settings.Current;
        MaxLogEntries = s.MaxLogEntries;
        MaxLogEntriesText = MaxLogEntries.ToString();
    }

    private void OnLogEntryWritten(object? sender, LogEntry entry)
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            // Enforce cap before adding to avoid unbounded growth
            var trimCount = 0;
            while (LogEntries.Count >= MaxLogEntries)
            {
                LogEntries.RemoveAt(0);
                trimCount++;
            }

            if (trimCount > 0)
                _log.Debug(LogSource, $"Trimmed {trimCount} log entries to maintain max limit");

            LogEntries.Add(entry);
        });
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogEntries.Clear();
    }

    [RelayCommand]
    private async Task OpenLogsFolderAsync()
    {
        var logDir = LoggingService.LogDirectory;
        if (Directory.Exists(logDir))
            await Launcher.OpenAsync(new Uri(logDir));
    }

        partial void OnMaxLogEntriesChanged(int oldValue, int newValue)
        {
            _log.Debug(LogSource, $"MaxLogEntries changed from {oldValue} to {newValue}");
            MaxLogEntriesText = newValue.ToString();
            TrimLogEntries();
            // Save to settings
            var s = _settings.Current;
            s.MaxLogEntries = newValue;
            _settings.Save(s);
        }

        private void TrimLogEntries()
        {
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                var trimCount = LogEntries.Count - MaxLogEntries;
                while (LogEntries.Count > MaxLogEntries)
                    LogEntries.RemoveAt(0);
                if (trimCount > 0)
                    _log.Debug(LogSource, $"Trimmed {trimCount} log entries via manual trim");
            });
        }
}
