using System.Text.Json.Serialization;

namespace LolClientHelper.Models;

public class Conversation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("participants")]
    public List<string> Participants { get; set; } = new();
}
