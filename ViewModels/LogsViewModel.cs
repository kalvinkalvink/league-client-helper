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
            MaxLogEntriesText = newValue.ToString();
            TrimLogEntries();
        }

        private void TrimLogEntries()
        {
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                while (LogEntries.Count > MaxLogEntries)
                    LogEntries.RemoveAt(0);
            });
        }
}
