using LolClientHelper.Models;

namespace LolClientHelper.Services;

/// <summary>
/// Detects the LeagueClientUx.exe process and extracts LCU credentials
/// from its command-line arguments.
/// Spec section 3.1.
/// </summary>
public interface ILeagueProcessService
{
    /// <summary>
    /// Attempts to find the LeagueClientUx.exe process and parse its
    /// <c>--app-port</c> and <c>--remoting-auth-token</c> arguments.
    /// Returns <c>null</c> if the process is not running or the arguments
    /// cannot be parsed.
    /// </summary>
    Task<LcuCredentials?> TryGetCredentialsAsync(CancellationToken ct = default);

    /// <summary>
    /// Polls indefinitely every <paramref name="retryIntervalMs"/> milliseconds
    /// until credentials are found or <paramref name="ct"/> is cancelled.
    /// Logs a WARNING each time the process is not yet found (spec §3.3).
    /// </summary>
    Task<LcuCredentials> WaitForCredentialsAsync(
        int retryIntervalMs = 10_000,
        CancellationToken ct = default);
}
