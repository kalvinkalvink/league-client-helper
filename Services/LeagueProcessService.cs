using System.Management;
using System.Text.RegularExpressions;
using LolClientHelper.Models;

namespace LolClientHelper.Services;

/// <summary>
/// Queries <c>Win32_Process</c> via WMI/CIM to locate LeagueClientUx.exe
/// and parse its command-line into <see cref="LcuCredentials"/>.
/// Requires the app to run as Administrator (spec §3.3 / app.manifest).
/// </summary>
public sealed partial class LeagueProcessService : ILeagueProcessService
{
    // -------------------------------------------------------------------------
    // Source name used in log entries
    // -------------------------------------------------------------------------
    private const string LogSource = "LeagueProcessService";

    // -------------------------------------------------------------------------
    // Pre-compiled regexes for arg parsing (spec §3.1)
    // -------------------------------------------------------------------------

    [GeneratedRegex(@"--app-port=(\d+)", RegexOptions.Compiled)]
    private static partial Regex PortRegex();

    [GeneratedRegex(@"--remoting-auth-token=([\w\-]+)", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    // -------------------------------------------------------------------------
    // Dependencies
    // -------------------------------------------------------------------------

    private readonly ILoggingService _log;

    public LeagueProcessService(ILoggingService log)
    {
        _log = log;
    }

    // -------------------------------------------------------------------------
    // ILeagueProcessService
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public Task<LcuCredentials?> TryGetCredentialsAsync(CancellationToken ct = default)
    {
        // WMI calls are synchronous; run on a thread-pool thread to keep the
        // UI responsive (spec §3.3 note on admin requirement).
        return Task.Run(() => QueryProcess(), ct);
    }

    /// <inheritdoc/>
    public async Task<LcuCredentials> WaitForCredentialsAsync(
        int retryIntervalMs = 10_000,
        CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var creds = await TryGetCredentialsAsync(ct).ConfigureAwait(false);
            if (creds is not null)
            {
                _log.Info(LogSource, $"Connected to LCU on port {creds.Port}, token: {creds.Token}");
                return creds;
            }

            // Spec §3.3 – log WARNING while waiting.
            _log.Warning(LogSource, "Waiting for League client to start...");

            await Task.Delay(retryIntervalMs, ct).ConfigureAwait(false);
        }

        ct.ThrowIfCancellationRequested();
        // Unreachable but satisfies the compiler.
        throw new OperationCanceledException(ct);
    }

    // -------------------------------------------------------------------------
    // Core WMI query
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs a synchronous WMI query for the LeagueClientUx.exe process and
    /// returns parsed credentials, or <c>null</c> if not found / parse fails.
    /// Uses <c>System.Management</c> which mirrors
    /// <c>Get-CimInstance Win32_Process</c> (spec §3.1).
    /// </summary>
    private LcuCredentials? QueryProcess()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT CommandLine FROM Win32_Process WHERE Name='LeagueClientUx.exe'");

            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    var commandLine = obj["CommandLine"]?.ToString();
                    if (string.IsNullOrWhiteSpace(commandLine))
                        continue;

                    var creds = ParseCommandLine(commandLine);
                    if (creds is not null)
                        return creds;
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(LogSource, "WMI query for LeagueClientUx.exe failed", ex);
        }

        return null;
    }

    /// <summary>
    /// Parses <c>--app-port</c> and <c>--remoting-auth-token</c> from the
    /// raw command-line string. Returns <c>null</c> if either is missing.
    /// </summary>
    private LcuCredentials? ParseCommandLine(string commandLine)
    {
        var portMatch = PortRegex().Match(commandLine);
        var tokenMatch = TokenRegex().Match(commandLine);

        if (!portMatch.Success || !tokenMatch.Success)
        {
            _log.Debug(LogSource, "LeagueClientUx.exe found but args not yet populated.");
            return null;
        }

        if (!int.TryParse(portMatch.Groups[1].Value, out var port) || port <= 0)
        {
            _log.Error(LogSource, $"Failed to parse --app-port value: '{portMatch.Groups[1].Value}'");
            return null;
        }

        var token = tokenMatch.Groups[1].Value;
        _log.Debug(LogSource, $"Parsed LCU credentials — port={port}");

        return new LcuCredentials { Port = port, Token = token };
    }
}
