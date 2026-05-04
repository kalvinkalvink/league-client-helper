using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LolClientHelper.Models;
using LolClientHelper.Services;

namespace LolClientHelper.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ILoggingService _log;
    private const string LogSource = "LogsViewModel";
    private const int MaxLogEntries = 1000;

    private readonly System.Text.StringBuilder _logBuilder = new();

    [ObservableProperty] private string logText = string.Empty;

    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    public LogsViewModel(ILoggingService log)
    {
        _log = log;
        _log.LogEntryWritten += OnLogEntryWritten;
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
        });
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _logBuilder.Clear();
        LogText = string.Empty;
        LogEntries.Clear();
    }
}