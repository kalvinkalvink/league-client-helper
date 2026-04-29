using System.Text.Json;
using LolClientHelper.Models;

namespace LolClientHelper.Services;

public interface ILcuApiService
{
    bool IsConfigured { get; }

    void Configure(LcuCredentials credentials);

    Task<string> GetGameflowPhaseRawAsync(CancellationToken ct = default);
    Task AcceptReadyCheckAsync(CancellationToken ct = default);
    Task StartMatchmakingSearchAsync(CancellationToken ct = default);
    Task ReconnectAsync(CancellationToken ct = default);
    Task SkipHonorAsync(CancellationToken ct = default);
    Task PlayAgainAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Invitation>> GetReceivedInvitationsAsync(CancellationToken ct = default);
    Task AcceptInvitationAsync(string invitationId, CancellationToken ct = default);
    Task InviteFriendsAsync(IEnumerable<long> summonerIds, CancellationToken ct = default);

    Task<IReadOnlyList<Friend>> GetFriendsAsync(CancellationToken ct = default);
    Task<SummonerInfo?> GetCurrentSummonerAsync(CancellationToken ct = default);

    Task UpdateChatMeAsync(string queueType, string tier, string division, string availability, CancellationToken ct = default);
    Task<JsonDocument> GetLoginSessionAsync(CancellationToken ct = default);
    Task<JsonDocument> GetRankedStatsAsync(string puuid, CancellationToken ct = default);
    Task<JsonDocument> GetMatchHistoryAsync(string puuid, CancellationToken ct = default);
}
