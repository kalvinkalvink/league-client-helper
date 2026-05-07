using LolClientHelper.Services;

namespace LolClientHelper
{
    public partial class App : Application
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly IGameStateService _gameStateService;
        private readonly MainPage _mainPage;

        public App(
            ISettingsService settingsService,
            ILocalizationService localizationService,
            IGameStateService gameStateService,
            MainPage mainPage)
        {
            InitializeComponent();

            _settingsService = settingsService;
            _localizationService = localizationService;
            _gameStateService = gameStateService;
            _mainPage = mainPage;

            var settings = _settingsService.Current;
            _localizationService.SetLanguage(settings.Language);
            UserAppTheme = settings.Theme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var titleBar = new TitleBar
            {
                Title = "LolClientHelper",
                Icon = "Resources/AppIcon/LeagueClientHelper.ico"
            };

            var window = new Window(_mainPage)
            {
                TitleBar = titleBar
            };

            _settingsService.RestoreWindowPosition(window);

            window.Destroying += async (_, _) =>
            {
                _settingsService.SaveWindowPosition(window);
                await _gameStateService.StopAsync();
            };

            _ = _gameStateService.StartAsync();
            return window;
        }
    }
}