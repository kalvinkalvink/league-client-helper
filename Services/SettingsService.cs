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

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private AppSettings _current = new();

    /// <inheritdoc/>
    public AppSettings Current => _current;

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
                return _current;
            }

            var json = File.ReadAllText(SettingsFilePath);
            _current = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions)
                       ?? new AppSettings();
        }
        catch (Exception)
        {
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
        }
        catch (Exception)
        {
            // Non-fatal — the app continues with in-memory settings.
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Applies the saved X, Y, Width, and Height values to <paramref name="window"/>.
    /// Spec section 10.4 – "On App Start: Restore window position/size".
    /// </remarks>
    public void RestoreWindowPosition(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.X = _current.WindowX;
        window.Y = _current.WindowY;
        window.Width = _current.WindowWidth;
        window.Height = _current.WindowHeight;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Captures the window's current geometry into <see cref="Current"/> and
    /// immediately persists to disk.
    /// Spec section 10.4 – "On App Close: Save window position".
    /// </remarks>
    public void SaveWindowPosition(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _current.WindowX = window.X;
        _current.WindowY = window.Y;
        _current.WindowWidth = window.Width;
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
