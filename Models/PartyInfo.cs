using System.Text.Json.Serialization;

namespace LolClientHelper.Models;

public sealed class PartyInfo
{
    [JsonPropertyName("agsActivityId")]
    public string AgsActivityId { get; init; } = string.Empty;

    [JsonPropertyName("isPartyOpen")]
    public bool IsPartyOpen { get; init; }

    [JsonPropertyName("maxPlayers")]
    public int MaxPlayers { get; init; }

    [JsonPropertyName("partyId")]
    public string PartyId { get; init; } = string.Empty;

    [JsonPropertyName("queueId")]
    public int QueueId { get; init; }

    [JsonPropertyName("summonerPuuids")]
    public List<string> SummonerPuuids { get; init; } = new();

    [JsonPropertyName("summoners")]
    public List<long> Summoners { get; init; } = new();
}
