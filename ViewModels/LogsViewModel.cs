using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;
using Microsoft.Maui.ApplicationModel;

namespace LolClientHelper.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ILoggingService _log;
    private readonly ILocalizationService _localization;
    private const string LogSource = "LogsViewModel";
    private const int MaxLogEntries = 1000;

    private readonly System.Text.StringBuilder _logBuilder = new();

    [ObservableProperty] private string logText = string.Empty;
    [ObservableProperty] private string filteredLogText = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;

    public string LogsLabel => _localization.Get("tab.logs");
    public string ClearLogsLabel => _localization.Get("logs.clear");
    public string OpenLogsFolderLabel => _localization.Get("logs.open_folder");
    public string SearchLogsPlaceholder => _localization.Get("logs.search_placeholder");

    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    public LogsViewModel(ILoggingService log, ILocalizationService localization)
    {
        _log = log;
        _localization = localization;
        _log.LogEntryWritten += OnLogEntryWritten;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
        UpdateFilteredLogText();
    }

    private void OnLogEntryWritten(object? sender, LogEntry entry)
    {
        var dispatcher = Application.Current?.Dispatcher;
        dispatcher?.Dispatch(() =>
        {
            _logBuilder.AppendLine(entry.ToString());
            LogText = _logBuilder.ToString();
            LogEntries.Add(entry);
            while (LogEntries.Count > MaxLogEntries)
                LogEntries.RemoveAt(0);
            UpdateFilteredLogText();
        });
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _logBuilder.Clear();
        LogText = string.Empty;
        LogEntries.Clear();
        UpdateFilteredLogText();
    }

    [RelayCommand]
    private async Task OpenLogsFolderAsync()
    {
        var logDir = LoggingService.LogDirectory;
        if (Directory.Exists(logDir))
        {
            await Launcher.OpenAsync(new Uri(logDir));
        }
    }

    private void UpdateFilteredLogText()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredLogText = LogText;
        }
        else
        {
            var searchLower = SearchText.ToLowerInvariant();
            var filtered = LogEntries
                .Where(e => e.ToString().ToLowerInvariant().Contains(searchLower))
                .Select(e => e.ToString());
            FilteredLogText = string.Join(Environment.NewLine, filtered);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        UpdateFilteredLogText();
    }
}