using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LolClientHelper.Models;

/// <summary>
/// A friend entry returned by GET /lol-chat/v1/friends.
/// Spec section 6.3.
/// </summary>
public partial class Friend : ObservableObject
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("gameName")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Friend's custom group name. Empty string means no group.</summary>
    [JsonPropertyName("groupName")]
    public string GroupName { get; init; } = string.Empty;

    /// <summary>Availability: chat, away, offline, mobile.</summary>
    [JsonPropertyName("availability")]
    public string Availability { get; init; } = string.Empty;

    [JsonPropertyName("puuid")]
    public string Puuid { get; init; } = string.Empty;

    [JsonPropertyName("summonerId")]
    public long SummonerId { get; init; }

    private string _gameStatus = string.Empty;
    /// <summary>Game status from webhook: inGame, outOfGame, hosting_*, etc.</summary>
    public string GameStatus
    {
        get => _gameStatus;
        set
        {
            if (SetProperty(ref _gameStatus, value))
                OnPropertyChanged(nameof(EffectiveAvailability));
        }
    }

    private string _product = string.Empty;
    /// <summary>Product from webhook: league_of_legends, etc.</summary>
    public string Product
    {
        get => _product;
        set
        {
            if (SetProperty(ref _product, value))
                OnPropertyChanged(nameof(EffectiveAvailability));
        }
    }

    /// <summary>
    /// Returns the effective availability for display:
    /// - inGame/in_game/playing/match → "inGame" (Orange)
    /// - hosting_* → "hosting" (Green)
    /// - chat + league_of_legends → "online" (Green)
    /// - chat + other product → "chat" (LightGreen)
    /// - away/offline/mobile → as-is with appropriate colors
    /// </summary>
    public string EffectiveAvailability
    {
        get
        {
            // 1. Check for in-game states (handle variations from Riot API)
            if (!string.IsNullOrEmpty(GameStatus))
            {
                var statusLower = GameStatus.ToLowerInvariant();
                
                // Match any in-game related status
                if (statusLower.Contains("ingame") || statusLower.Contains("playing") || 
                    statusLower.Contains("match") || statusLower == "ingame")
                    return "inGame";
                
                // 2. If hosting, show "hosting" (Green)
                if (GameStatus.StartsWith("hosting_", StringComparison.OrdinalIgnoreCase))
                    return "hosting";
                
                // 3. If not outOfGame, return the raw status
                if (!statusLower.Equals("outofgame", StringComparison.OrdinalIgnoreCase))
                    return GameStatus;
            }

            // 4. For outOfGame or no gameStatus, check availability + product
            // If availability is "chat" and product is "league_of_legends" → "online" (Green)
            if (string.Equals(Availability, "chat", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Product, "league_of_legends", StringComparison.OrdinalIgnoreCase))
                return "online";

            // 5. Otherwise return the raw availability (chat/away/offline/mobile)
            return Availability;
        }
    }
}
