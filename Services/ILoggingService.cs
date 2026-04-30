namespace LolClientHelper.Services;

/// <summary>
/// Represents a single log entry.
/// </summary>
/// <param name="Timestamp">When the log was written.</param>
/// <param name="Level">Log level (DEBUG, INFO, WARNING, ERROR).</param>
/// <param name="Source">Component that wrote the log.</param>
/// <param name="Message">Log message.</param>
public sealed record LogEntry(DateTime Timestamp, string Level, string Source, string Message);

/// <summary>
/// Provides structured, rolling-file logging.
/// Spec section 7.
/// </summary>
public interface ILoggingService
{
    /// <summary>Emits a DEBUG-level entry (API details, token parsing, state changes).</summary>
    void Debug(string source, string message);

    /// <summary>Emits an INFO-level entry (connection status, feature triggers).</summary>
    void Info(string source, string message);

    /// <summary>Emits a WARNING-level entry (retry attempts, non-fatal errors).</summary>
    void Warning(string source, string message);

    /// <summary>Emits an ERROR-level entry (API failures, process detection failures).</summary>
    void Error(string source, string message, Exception? exception = null);

    /// <summary>
    /// Controls whether DEBUG entries are written.
    /// Mirrors <see cref="Models.AppSettings.DebugLogging"/>.
    /// </summary>
    bool IsDebugEnabled { get; set; }

    /// <summary>Raised when a log entry is written.</summary>
    event EventHandler<LogEntry>? LogEntryWritten;
}
