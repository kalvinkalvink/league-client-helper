namespace LolClientHelper.Models;

/// <summary>
/// LCU credentials parsed from the LeagueClientUx.exe command-line arguments.
/// Spec section 3.1.
/// </summary>
public sealed class LcuCredentials
{
    /// <summary>
    /// The port the LCU REST API is listening on (--app-port).
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    /// The remoting auth token used for Basic authentication
    /// as riot:{Token} (--remoting-auth-token).
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Builds the base URL for LCU API calls.
    /// </summary>
    public string BaseUrl => $"https://127.0.0.1:{Port}";

    /// <summary>
    /// Returns the Base64-encoded Basic auth header value (riot:{Token}).
    /// </summary>
    public string BasicAuthHeader =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"riot:{Token}"));
}
