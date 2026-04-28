using LolClientHelper.Services;
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

        // -----------------------------------------------------------------
        // Debug logging from Microsoft.Extensions.Logging (dev builds only)
        // -----------------------------------------------------------------

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
