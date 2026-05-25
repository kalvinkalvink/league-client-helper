using System.Text.Json;
using LolClientHelper.Models;

namespace LolClientHelper.Services;

/// <summary>
/// Loads and persists <see cref="AppSettings"/> as JSON in
/// <c>%APPDATA%\LolClientHelper\settings.json</c>.
/// Spec section 10.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "LolClientHelper");

    private static readonly string SettingsFilePath =
        Path.Combine(AppDataDir, "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private const string LogSource = "SettingsService";
    private readonly ILoggingService _log;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private AppSettings _current = new();

    /// <inheritdoc/>
    public AppSettings Current => _current;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public SettingsService(ILoggingService log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        Load();
    }

    // -------------------------------------------------------------------------
    // ISettingsService
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public AppSettings Load()
    {
        try
        {
            EnsureDirectoryExists();

            if (!File.Exists(SettingsFilePath))
            {
                _current = new AppSettings();
                _log.Info(LogSource, "No settings file found, using default settings.");
                return _current;
            }

            var json = File.ReadAllText(SettingsFilePath);
            _current = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions)
                       ?? new AppSettings();
            _log.Info(LogSource, $"Successfully loaded settings from {SettingsFilePath}");
        }
        catch (Exception ex)
        {
            _log.Warning(LogSource, $"Failed to load settings: {ex.Message}");
            // If the file is corrupt or unreadable, fall back to defaults.
            _current = new AppSettings();
        }

        return _current;
    }

    /// <inheritdoc/>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _current = settings;

        try
        {
            EnsureDirectoryExists();
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(SettingsFilePath, json);
            _log.Debug(LogSource, $"Successfully saved settings to {SettingsFilePath}");
        }
        catch (Exception ex)
        {
            _log.Warning(LogSource, $"Failed to save settings: {ex.Message}");
            // Non-fatal — the app continues with in-memory settings.
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Applies the saved X, Y, Width, and Height values to <paramref name="window"/>.
    /// Spec section 10.4 – "On App Start: Restore window position/size".
    /// Validates that saved values are within sane bounds to prevent the window
    /// from being placed off-screen (e.g. after saving a minimized state).
    /// </remarks>
    public void RestoreWindowPosition(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Sanity bounds — prevent off-screen placement from corrupt saved state.
        const double minPos = -10;
        const double maxPos = 5000;
        const double minSize = 200;
        const double maxSize = 5000;

        if (_current.WindowX is >= minPos and <= maxPos)
            window.X = _current.WindowX;

        if (_current.WindowY is >= minPos and <= maxPos)
            window.Y = _current.WindowY;

        if (_current.WindowWidth is >= minSize and <= maxSize)
            window.Width = _current.WindowWidth;

        if (_current.WindowHeight is >= minSize and <= maxSize)
            window.Height = _current.WindowHeight;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Captures the window's current geometry into <see cref="Current"/> and
    /// immediately persists to disk.
    /// Only saves values within sane bounds to prevent persisting a minimized
    /// or off-screen state.
    /// Spec section 10.4 – "On App Close: Save window position".
    /// </remarks>
    public void SaveWindowPosition(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Same sanity bounds used in RestoreWindowPosition.
        const double minPos = -10;
        const double maxPos = 5000;
        const double minSize = 200;
        const double maxSize = 5000;

        if (window.X is >= minPos and <= maxPos)
            _current.WindowX = window.X;

        if (window.Y is >= minPos and <= maxPos)
            _current.WindowY = window.Y;

        if (window.Width is >= minSize and <= maxSize)
            _current.WindowWidth = window.Width;

        if (window.Height is >= minSize and <= maxSize)
            _current.WindowHeight = window.Height;

        Save(_current);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(AppDataDir))
            Directory.CreateDirectory(AppDataDir);
    }
}
