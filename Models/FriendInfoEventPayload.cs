using System.Text.Json.Serialization;

namespace LolClientHelper.Models;

public sealed class FriendInfoEventPayload
{
    [JsonPropertyName("data")]
    public FriendInfoData Data { get; init; } = new();

    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; init; } = string.Empty;
}
