namespace LolClientHelper.Models;

/// <summary>
/// Game flow phase states as returned by /lol-gameflow/v1/gameflow-phase.
/// Spec section 4.
/// </summary>
public enum GameState
{
    Unknown,
    /// <summary>In lobby / main menu.</summary>
    MainMenu,

    Lobby,
    /// <summary>Searching for a match.</summary>
    Matchmaking,

    /// <summary>Match found — ready-check screen.</summary>
    ReadyCheck,

    /// <summary>Champion selection.</summary>
    ChampSelect,

    /// <summary>Game is actively in progress.</summary>
    InProgress,

    /// <summary>Post-game honor screen.</summary>
    PreEndOfGame,

    /// <summary>Waiting for stats page to load.</summary>
    WaitingForStats,

    /// <summary>End-of-game; player can re-queue.</summary>
    EndOfGame,

    /// <summary>Player must reconnect to an in-progress game.</summary>
    Reconnect
}
