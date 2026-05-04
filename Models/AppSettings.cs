namespace LolClientHelper.Models;

/// <summary>
/// User-configurable application settings.
/// Persisted to %APPDATA%\LolClientHelper\settings.json.
/// Spec section 10.2.
/// </summary>
public sealed class AppSettings
{
    // -------------------------------------------------------------------------
    // Window State
    // -------------------------------------------------------------------------

    public double WindowX { get; set; } = 100;
    public double WindowY { get; set; } = 100;
    public double WindowWidth { get; set; } = 1000;
    public double WindowHeight { get; set; } = 800;

    // -------------------------------------------------------------------------
    // Game Auto features (spec section 6.1)
    // -------------------------------------------------------------------------

    /// <summary>Auto-search for a match when in lobby.</summary>
    public bool AutoStartGame { get; set; } = false;

    /// <summary>Automatically accept the ready-check popup.</summary>
    public bool AutoAcceptMatch { get; set; } = true;

    /// <summary>Automatically skip the post-game honor screen.</summary>
    public bool AutoSkipLike { get; set; } = true;

    /// <summary>Automatically return to lobby after a game ends.</summary>
    public bool AutoReenterLobby { get; set; } = true;

    /// <summary>Automatically reconnect to a disconnected game.</summary>
    public bool AutoReconnect { get; set; } = false;

    // -------------------------------------------------------------------------
    // Main Page
    // -------------------------------------------------------------------------

    /// <summary>Automatically accept incoming game invitations.</summary>
    public bool AutoAcceptInvite { get; set; } = true;

    // -------------------------------------------------------------------------
    // Lobby
    // -------------------------------------------------------------------------

    /// <summary>Filter friend invitations by this group name ("All" = no filter).</summary>
    public string FriendFilterGroup { get; set; } = "All";

    // -------------------------------------------------------------------------
    // Game Status — Fake Rank (spec section 6.2)
    // -------------------------------------------------------------------------

    /// <summary>Queue type shown on the fake rank: RANKED_SOLO_5x5, RANKED_FLEX_SR, etc.</summary>
    public string QueueType { get; set; } = "RANKED_SOLO_5x5";

    /// <summary>Tier shown on the fake rank: IRON … CHALLENGER.</summary>
    public string Tier { get; set; } = "CHALLENGER";

    /// <summary>Division shown on the fake rank: IV, III, II, I.</summary>
    public string Division { get; set; } = "I";

    /// <summary>Chat availability status: chat, away, dnd, offline, mobile.</summary>
    public string Status { get; set; } = "chat";

    // -------------------------------------------------------------------------
    // Advanced Settings
    // -------------------------------------------------------------------------

    /// <summary>
    /// HTTP polling interval in milliseconds when WebSocket is unavailable.
    /// Default 500 ms (spec section 4).
    /// </summary>
    public int PollIntervalMs { get; set; } = 500;

    /// <summary>Launch the application minimised to the taskbar.</summary>
    public bool StartMinimized { get; set; } = false;

    // -------------------------------------------------------------------------
    // Appearance (spec section 8.2)
    // -------------------------------------------------------------------------

    /// <summary>UI theme: Auto, Light, or Dark. Default is Dark.</summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>Apply fake-rank settings immediately on application start.</summary>
    public bool ChangeRankingOnStart { get; set; } = false;

    // -------------------------------------------------------------------------
    // Localization (spec section 14)
    // -------------------------------------------------------------------------

    /// <summary>UI language code: en, zh-CN, zh-TW.</summary>
    public string Language { get; set; } = "en";

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    /// <summary>Enable DEBUG-level log entries (spec section 7.2).</summary>
    public bool DebugLogging { get; set; } = false;
}
