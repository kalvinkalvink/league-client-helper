using System.Text.Json.Serialization;

namespace LolClientHelper.Models;

/// <summary>
/// Summoner information returned by /lol-summoner/v1/current-summoner.
/// Spec section 5.1.
/// </summary>
public sealed class SummonerInfo
{
    /// <summary>Unique persistent summoner ID.</summary>
    [JsonPropertyName("summonerId")]
    public long SummonerId { get; init; }

    /// <summary>Account ID.</summary>
    [JsonPropertyName("accountId")]
    public long AccountId { get; init; }

    /// <summary>Riot PUUID (used for ranked stats and match history).</summary>
    [JsonPropertyName("puuid")]
    public string Puuid { get; init; } = string.Empty;

    /// <summary>Display name shown in the client.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Internal profile icon ID.</summary>
    [JsonPropertyName("profileIconId")]
    public int ProfileIconId { get; init; }

    /// <summary>Summoner level.</summary>
    [JsonPropertyName("summonerLevel")]
    public int SummonerLevel { get; init; }
}
