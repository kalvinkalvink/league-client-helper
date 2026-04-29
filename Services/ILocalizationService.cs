namespace LolClientHelper.Services;

public interface ILocalizationService
{
    event EventHandler? LanguageChanged;

    string CurrentLanguageCode { get; }

    string Get(string key);

    void SetLanguage(string languageCode);
}
