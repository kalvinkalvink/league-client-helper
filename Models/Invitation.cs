using System.Text.Json.Serialization;

namespace LolClientHelper.Models;

/// <summary>
/// An incoming lobby invitation returned by
/// GET /lol-lobby/v2/received-invitations.
/// Spec section 5.1 and 6.1 (AutoAcceptInvite).
/// </summary>
public sealed class Invitation
{
    [JsonPropertyName("invitationId")]
    public string InvitationId { get; init; } = string.Empty;

    [JsonPropertyName("fromSummonerId")]
    public long FromSummonerId { get; init; }

    [JsonPropertyName("fromSummonerName")]
    public string FromSummonerName { get; init; } = string.Empty;

    /// <summary>Current state: Pending, Accepted, Declined, etc.</summary>
    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    /// <summary>Timestamp string from the LCU (ISO 8601).</summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;
}
