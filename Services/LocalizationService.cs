using System.Globalization;
using System.Resources;

namespace LolClientHelper.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string LogSource = "LocalizationService";
    private readonly ILoggingService _log;
    private static readonly ResourceManager ResourceManager = new("LolClientHelper.Resources.Strings.Strings", typeof(LocalizationService).Assembly);

    public LocalizationService(ILoggingService log)
    {
        _log = log;
        CurrentLanguageCode = CultureInfo.CurrentUICulture.Name;
    }

    public event EventHandler? LanguageChanged;
    public string CurrentLanguageCode { get; private set; }

    public string Get(string key)
    {
        var value = ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        return string.IsNullOrWhiteSpace(value) ? key : value;
    }

    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return;

        var culture = new CultureInfo(languageCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CurrentLanguageCode = languageCode;

        _log.Info(LogSource, $"Language switched to {languageCode}");
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
