using System.Text.Json.Serialization;

namespace LolClientHelper.Models;

/// <summary>
/// A friend entry returned by GET /lol-chat/v1/friends.
/// Spec section 6.3.
/// </summary>
public sealed class Friend
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Friend's custom group name. Empty string means no group.</summary>
    [JsonPropertyName("groupName")]
    public string GroupName { get; init; } = string.Empty;

    /// <summary>Availability: chat, away, dnd, offline, mobile.</summary>
    [JsonPropertyName("availability")]
    public string Availability { get; init; } = string.Empty;

    [JsonPropertyName("puuid")]
    public string Puuid { get; init; } = string.Empty;

    [JsonPropertyName("summonerId")]
    public long SummonerId { get; init; }
}
