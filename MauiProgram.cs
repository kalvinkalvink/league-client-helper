using LolClientHelper.Services;
using LolClientHelper.ViewModels;
using LolClientHelper.Views;
using Microsoft.Extensions.Logging;

namespace LolClientHelper;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // -----------------------------------------------------------------
        // Infrastructure services (spec sections 7 & 10)
        // -----------------------------------------------------------------

        // Logging — singleton; must be registered first so other services
        // can inject ILoggingService via the DI container.
        builder.Services.AddSingleton<ILoggingService, LoggingService>();

        // Settings — singleton; loaded once at startup.
        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        // LCU communication layer (spec sections 3, 4, 5).
        builder.Services.AddSingleton<ILeagueProcessService, LeagueProcessService>();
        builder.Services.AddSingleton<ILcuApiService, LcuApiService>();
        builder.Services.AddSingleton<IWebSocketService, WebSocketService>();
        builder.Services.AddSingleton<IGameStateService, GameStateService>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddSingleton<IWindowService, WindowService>();

        builder.Services.AddSingleton<GameAutoViewModel>();
        builder.Services.AddSingleton<MainPageTabViewModel>();
        builder.Services.AddSingleton<LobbyViewModel>();
        builder.Services.AddSingleton<GameStatusViewModel>();
        builder.Services.AddSingleton<LogsViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<ChampSelectViewModel>();
        builder.Services.AddSingleton<EndOfGameViewModel>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<SettingsWindow>();

        // -----------------------------------------------------------------
        // Debug logging from Microsoft.Extensions.Logging (dev builds only)
        // -----------------------------------------------------------------

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
