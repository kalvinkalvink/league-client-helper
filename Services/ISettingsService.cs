using LolClientHelper.Models;

namespace LolClientHelper.Services;

/// <summary>
/// Provides persistent storage and retrieval of <see cref="AppSettings"/>.
/// Spec section 10.3.
/// </summary>
public interface ISettingsService
{
    /// <summary>The currently loaded settings instance.</summary>
    AppSettings Current { get; }

    /// <summary>
    /// Loads settings from disk. Returns defaults if the file does not exist.
    /// </summary>
    AppSettings Load();

    /// <summary>
    /// Persists the given settings to disk immediately.
    /// </summary>
    void Save(AppSettings settings);

    /// <summary>
    /// Restores window position and size from <see cref="Current"/>.
    /// </summary>
    void RestoreWindowPosition(Window window);

    /// <summary>
    /// Captures the window's current position and size into <see cref="Current"/>
    /// and persists immediately.
    /// </summary>
    void SaveWindowPosition(Window window);
}
