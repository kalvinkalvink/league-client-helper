namespace LolClientHelper.Services;

/// <summary>
/// Rolling-file logger that writes one file per day under
/// <c>%APPDATA%\LolClientHelper\logs\</c> and prunes files older than 7 days.
/// Spec section 7.
/// </summary>
public sealed class LoggingService : ILoggingService, IDisposable
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private static readonly string LogDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "LolClientHelper", "logs");

    private const int RetentionDays = 7;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private readonly object _lock = new();
    private StreamWriter? _writer;
    private DateOnly _currentLogDate;
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsDebugEnabled { get; set; }

    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    public LoggingService()
    {
        EnsureDirectoryExists();
        PruneOldLogs();
        OpenLogFile(DateOnly.FromDateTime(DateTime.Now));
    }

    // -------------------------------------------------------------------------
    // ILoggingService
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public void Debug(string source, string message)
    {
        if (IsDebugEnabled)
            Write("DEBUG", source, message);
    }

    /// <inheritdoc/>
    public void Info(string source, string message) =>
        Write("INFO", source, message);

    /// <inheritdoc/>
    public void Warning(string source, string message) =>
        Write("WARNING", source, message);

    /// <inheritdoc/>
    public void Error(string source, string message, Exception? exception = null)
    {
        var fullMessage = exception is null
            ? message
            : $"{message} | {exception.GetType().Name}: {exception.Message}";
        Write("ERROR", source, fullMessage);
    }

    // -------------------------------------------------------------------------
    // Core write
    // -------------------------------------------------------------------------

    /// <summary>
    /// Formats and appends a log line. Rolls to a new file at day boundaries.
    /// Format: <c>[yyyy-MM-dd HH:mm:ss.fff] [LEVEL] [Source] Message</c>
    /// Spec section 7.3.
    /// </summary>
    private void Write(string level, string source, string message)
    {
        if (_disposed) return;

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var line = $"[{now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{source}] {message}";

        lock (_lock)
        {
            if (_disposed) return;

            // Roll to a new file if the calendar date has changed.
            if (today != _currentLogDate)
            {
                CloseWriter();
                PruneOldLogs();
                OpenLogFile(today);
            }

            try
            {
                _writer?.WriteLine(line);
                _writer?.Flush();
            }
            catch
            {
                // Swallow — logging must never crash the application.
            }
        }
    }

    // -------------------------------------------------------------------------
    // File management
    // -------------------------------------------------------------------------

    private void OpenLogFile(DateOnly date)
    {
        _currentLogDate = date;
        var path = Path.Combine(LogDir, $"log-{date:yyyy-MM-dd}.txt");
        try
        {
            _writer = new StreamWriter(path, append: true, System.Text.Encoding.UTF8)
            {
                AutoFlush = false
            };
        }
        catch
        {
            _writer = null;
        }
    }

    private void CloseWriter()
    {
        try { _writer?.Flush(); } catch { /* ignored */ }
        try { _writer?.Dispose(); } catch { /* ignored */ }
        _writer = null;
    }

    /// <summary>
    /// Deletes log files that are older than <see cref="RetentionDays"/> days.
    /// Spec section 7.4.
    /// </summary>
    private static void PruneOldLogs()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-RetentionDays);
            foreach (var file in Directory.EnumerateFiles(LogDir, "log-*.txt"))
            {
                var info = new FileInfo(file);
                if (info.LastWriteTime < cutoff)
                {
                    try { info.Delete(); } catch { /* ignored */ }
                }
            }
        }
        catch
        {
            // Non-fatal.
        }
    }

    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(LogDir))
            Directory.CreateDirectory(LogDir);
    }

    // -------------------------------------------------------------------------
    // IDisposable
    // -------------------------------------------------------------------------

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            CloseWriter();
        }
    }
}
