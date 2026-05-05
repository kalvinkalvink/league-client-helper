using System.Text.Json.Serialization;

namespace LolClientHelper.Models;

public sealed class FriendLolInfo
{
    [JsonPropertyName("bannerIdSelected")]
    public string BannerIdSelected { get; init; } = string.Empty;

    [JsonPropertyName("challengeCrystalLevel")]
    public string ChallengeCrystalLevel { get; init; } = string.Empty;

    [JsonPropertyName("challengePoints")]
    public string ChallengePoints { get; init; } = string.Empty;

    [JsonPropertyName("challengeTokensSelected")]
    public string ChallengeTokensSelected { get; init; } = string.Empty;

    [JsonPropertyName("championId")]
    public string ChampionId { get; init; } = string.Empty;

    [JsonPropertyName("companionId")]
    public string CompanionId { get; init; } = string.Empty;

    [JsonPropertyName("damageSkinId")]
    public string DamageSkinId { get; init; } = string.Empty;

    [JsonPropertyName("gameMode")]
    public string GameMode { get; init; } = string.Empty;

    [JsonPropertyName("gameQueueType")]
    public string GameQueueType { get; init; } = string.Empty;

    [JsonPropertyName("gameStatus")]
    public string GameStatus { get; init; } = string.Empty;

    [JsonPropertyName("iconOverride")]
    public string IconOverride { get; init; } = string.Empty;

    [JsonPropertyName("isObservable")]
    public string IsObservable { get; init; } = string.Empty;

    [JsonPropertyName("legendaryMasteryScore")]
    public string LegendaryMasteryScore { get; init; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; init; } = string.Empty;

    [JsonPropertyName("mapId")]
    public string MapId { get; init; } = string.Empty;

    [JsonPropertyName("mapSkinId")]
    public string MapSkinId { get; init; } = string.Empty;

    [JsonPropertyName("playerTitleSelected")]
    public string PlayerTitleSelected { get; init; } = string.Empty;

    [JsonPropertyName("pty")]
    public string Pty { get; init; } = string.Empty;

    [JsonPropertyName("ptyType")]
    public string PtyType { get; init; } = string.Empty;

    [JsonPropertyName("puuid")]
    public string Puuid { get; init; } = string.Empty;

    [JsonPropertyName("queueId")]
    public string QueueId { get; init; } = string.Empty;

    [JsonPropertyName("rankedPrevSeasonDivision")]
    public string RankedPrevSeasonDivision { get; init; } = string.Empty;

    [JsonPropertyName("rankedPrevSeasonTier")]
    public string RankedPrevSeasonTier { get; init; } = string.Empty;

    [JsonPropertyName("regalia")]
    public string Regalia { get; init; } = string.Empty;

    [JsonPropertyName("skinVariant")]
    public string SkinVariant { get; init; } = string.Empty;

    [JsonPropertyName("skinname")]
    public string Skinname { get; init; } = string.Empty;
}
