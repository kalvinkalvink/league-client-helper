using System.Text.Json.Serialization;

namespace LolClientHelper.Models;

/// <summary>
/// Game session data returned by /lol-gameflow/v1/session.
/// </summary>
public sealed class GameFlowSession
{
    [JsonPropertyName("gameData")]
    public GameData GameData { get; init; } = new();
}

public sealed class GameData
{
    [JsonPropertyName("teamPlayers")]
    public List<GameSessionPlayer> TeamPlayers { get; init; } = [];
}

public sealed class GameSessionPlayer
{
    [JsonPropertyName("puuid")]
    public string Puuid { get; init; } = string.Empty;
}