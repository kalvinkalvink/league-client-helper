using System.Text.Json.Serialization;

namespace LolClientHelper.Models;

public sealed class FriendInfoData
{
    [JsonPropertyName("accountId")]
    public long AccountId { get; init; }

    [JsonPropertyName("availability")]
    public string Availability { get; init; } = string.Empty;

    [JsonPropertyName("discordId")]
    public string? DiscordId { get; init; }

    [JsonPropertyName("discordOnlineStatus")]
    public string? DiscordOnlineStatus { get; init; }

    [JsonPropertyName("gameName")]
    public string GameName { get; init; } = string.Empty;

    [JsonPropertyName("gameTag")]
    public string GameTag { get; init; } = string.Empty;

    [JsonPropertyName("icon")]
    public int Icon { get; init; }

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("legendaryMasteryScore")]
    public int LegendaryMasteryScore { get; init; }

    [JsonPropertyName("lol")]
    public FriendLolInfo? Lol { get; init; }

    [JsonPropertyName("masteryScore")]
    public int MasteryScore { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("note")]
    public string Note { get; init; } = string.Empty;

    [JsonPropertyName("partySummoners")]
    public List<string> PartySummoners { get; init; } = new();

    [JsonPropertyName("patchline")]
    public string Patchline { get; init; } = string.Empty;

    [JsonPropertyName("platformId")]
    public string PlatformId { get; init; } = string.Empty;

    [JsonPropertyName("product")]
    public string Product { get; init; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; init; } = string.Empty;

    [JsonPropertyName("puuid")]
    public string Puuid { get; init; } = string.Empty;

    [JsonPropertyName("relationshipOnRiot")]
    public string RelationshipOnRiot { get; init; } = string.Empty;

    [JsonPropertyName("remotePlatform")]
    public bool RemotePlatform { get; init; }

    [JsonPropertyName("remoteProduct")]
    public bool RemoteProduct { get; init; }

    [JsonPropertyName("remoteProductBackdropUrl")]
    public string RemoteProductBackdropUrl { get; init; } = string.Empty;

    [JsonPropertyName("remoteProductIconUrl")]
    public string RemoteProductIconUrl { get; init; } = string.Empty;

    [JsonPropertyName("statusMessage")]
    public string StatusMessage { get; init; } = string.Empty;

    [JsonPropertyName("summonerIcon")]
    public int SummonerIcon { get; init; }

    [JsonPropertyName("summonerId")]
    public long SummonerId { get; init; }

    [JsonPropertyName("summonerLevel")]
    public int SummonerLevel { get; init; }
}
