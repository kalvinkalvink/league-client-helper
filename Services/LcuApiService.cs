using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using LolClientHelper.Models;

namespace LolClientHelper.Services;

public sealed class LcuApiService : ILcuApiService, IDisposable
{
    private const string LogSource = "LcuApiService";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILoggingService _log;
    private HttpClient _httpClient;
    private LcuCredentials? _credentials;
    private bool _disposed;

    public LcuApiService(ILoggingService log)
    {
        _log = log;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = ValidateLocalCertificate
        };

        _httpClient = new HttpClient(handler, disposeHandler: true);
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public bool IsConfigured => _credentials is not null;
    public LcuCredentials? Credentials => _credentials;

    public void Configure(LcuCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _credentials = credentials;

        // Dispose old HttpClient and create new one to avoid immutability issues
        // (HttpClient properties cannot be modified after first request)
        _httpClient.Dispose();
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = ValidateLocalCertificate
        };
        _httpClient = new HttpClient(handler, disposeHandler: true);
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.BaseAddress = new Uri(credentials.BaseUrl);

        _log.Info(LogSource, $"Configured API client for {credentials.BaseUrl}");
    }

    public Task<string> GetGameflowPhaseRawAsync(CancellationToken ct = default) =>
        GetStringAsync("/lol-gameflow/v1/gameflow-phase", ct);

    public Task AcceptReadyCheckAsync(CancellationToken ct = default) =>
        SendNoBodyAsync(HttpMethod.Post, "/lol-matchmaking/v1/ready-check/accept", ct);

    public Task StartMatchmakingSearchAsync(CancellationToken ct = default) =>
        SendNoBodyAsync(HttpMethod.Post, "/lol-lobby/v2/lobby/matchmaking/search", ct);

    public Task ReconnectAsync(CancellationToken ct = default) =>
        SendNoBodyAsync(HttpMethod.Post, "/lol-gameflow/v1/reconnect", ct);

    public Task SkipHonorAsync(CancellationToken ct = default) =>
        SendNoBodyAsync(HttpMethod.Post, "/lol-honor-v2/v1/honor-player/", ct);

    public Task PlayAgainAsync(CancellationToken ct = default) =>
        SendNoBodyAsync(HttpMethod.Post, "/lol-lobby/v2/play-again", ct);

    public async Task<IReadOnlyList<Invitation>> GetReceivedInvitationsAsync(CancellationToken ct = default)
    {
        var json = await GetStringAsync("/lol-lobby/v2/received-invitations", ct).ConfigureAwait(false);
        var invitations = JsonSerializer.Deserialize<List<Invitation>>(json, JsonOptions) ?? [];
        return invitations;
    }

    public Task AcceptInvitationAsync(string invitationId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(invitationId))
            throw new ArgumentException("Invitation id is required.", nameof(invitationId));

        return SendNoBodyAsync(
            HttpMethod.Post,
            $"/lol-lobby/v2/received-invitations/{Uri.EscapeDataString(invitationId)}/accept",
            ct);
    }

    public Task InviteFriendsAsync(IEnumerable<long> summonerIds, CancellationToken ct = default)
    {
        var ids = (summonerIds ?? []).ToList();
        var payload = JsonSerializer.Serialize(ids.Select(id => new { toSummonerId = id }));
        return SendJsonAsync(HttpMethod.Post, "/lol-lobby/v2/lobby/invitations", payload, ct);
    }

    public async Task<IReadOnlyList<Friend>> GetFriendsAsync(CancellationToken ct = default)
    {
        var json = await GetStringAsync("/lol-chat/v1/friends", ct).ConfigureAwait(false);
        var friends = JsonSerializer.Deserialize<List<Friend>>(json, JsonOptions) ?? [];
        return friends;
    }

    public async Task<SummonerInfo?> GetCurrentSummonerAsync(CancellationToken ct = default)
    {
        var json = await GetStringAsync("/lol-summoner/v1/current-summoner", ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SummonerInfo>(json, JsonOptions);
    }

    public Task UpdateChatMeAsync(string queueType, string tier, string division, string availability, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            availability,
            lol = new
            {
                rankedLeagueQueue = queueType,
                rankedLeagueTier = tier.ToUpperInvariant(),
                rankedLeagueDivision = division
            }
        });

        return SendJsonAsync(HttpMethod.Put, "/lol-chat/v1/me", payload, ct);
    }

    public Task<JsonDocument> GetLoginSessionAsync(CancellationToken ct = default) =>
        GetJsonDocumentAsync("/lol-login/v1/session", ct);

    public Task<JsonDocument> GetRankedStatsAsync(string puuid, CancellationToken ct = default) =>
        GetJsonDocumentAsync($"/lol-ranked/v1/ranked-stats/{Uri.EscapeDataString(puuid)}", ct);

    public Task<JsonDocument> GetMatchHistoryAsync(string puuid, CancellationToken ct = default) =>
        GetJsonDocumentAsync($"/lol-match-history/v1/products/lol/{Uri.EscapeDataString(puuid)}/matches", ct);

    private async Task<string> GetStringAsync(string endpoint, CancellationToken ct)
    {
        EnsureConfigured();
        using var req = new HttpRequestMessage(HttpMethod.Get, endpoint);
        req.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", _credentials!.BasicAuthHeader);
        using var rsp = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
        var content = await rsp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!rsp.IsSuccessStatusCode)
        {
            _log.Warning(LogSource, $"GET {endpoint} -> {(int)rsp.StatusCode} {rsp.StatusCode}");
            throw new HttpRequestException($"LCU GET failed: {endpoint}", null, rsp.StatusCode);
        }

        _log.Debug(LogSource, $"GET {endpoint} -> {(int)rsp.StatusCode}");
        return content;
    }

    private async Task<JsonDocument> GetJsonDocumentAsync(string endpoint, CancellationToken ct)
    {
        var json = await GetStringAsync(endpoint, ct).ConfigureAwait(false);
        return JsonDocument.Parse(json);
    }

    private Task SendNoBodyAsync(HttpMethod method, string endpoint, CancellationToken ct) =>
        SendJsonAsync(method, endpoint, "{}", ct);

    private Task SendJsonAsync(HttpMethod method, string endpoint, byte[] payload, CancellationToken ct) =>
        SendJsonAsync(method, endpoint, Encoding.UTF8.GetString(payload), ct);

    private async Task SendJsonAsync(HttpMethod method, string endpoint, string jsonPayload, CancellationToken ct)
    {
        EnsureConfigured();
        var payload = Encoding.UTF8.GetBytes(jsonPayload);

        using var req = new HttpRequestMessage(method, endpoint)
        {
            Content = new ByteArrayContent(payload)
        };
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        req.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", _credentials!.BasicAuthHeader);

        using var rsp = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
        if (!rsp.IsSuccessStatusCode)
        {
            var body = await rsp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _log.Warning(LogSource, $"{method} {endpoint} -> {(int)rsp.StatusCode} {rsp.StatusCode} | {body}");
            throw new HttpRequestException($"LCU {method} failed: {endpoint}", null, rsp.StatusCode);
        }

        _log.Debug(LogSource, $"{method} {endpoint} -> {(int)rsp.StatusCode}");
    }

    private void EnsureConfigured()
    {
        if (_credentials is null)
            throw new InvalidOperationException("LCU credentials are not configured.");
    }

    private static bool ValidateLocalCertificate(HttpRequestMessage message, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors)
    {
        var host = message.RequestUri?.Host;
        if (string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return errors == SslPolicyErrors.None;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _httpClient.Dispose();
    }
}
