using LolClientHelper.Services;
using LolClientHelper.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LolClientHelper.Views;

public partial class SettingsWindow : ContentPage
{
    private static Window? _instance;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public static void ShowSingleton(IServiceProvider services, ILoggingService log)
    {
        if (_instance is null)
        {
            var page = services.GetRequiredService<SettingsWindow>();
            _instance = new Window(page)
            {
                Title = "Settings",
                Width = 420,
                Height = 460
            };

            _instance.Destroying += (_, _) => _instance = null;
            Application.Current?.OpenWindow(_instance);
            return;
        }

#if WINDOWS
        if (_instance.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            nativeWindow.Activate();
#endif
        log.Info("SettingsWindow", "Settings window already open; focusing existing instance.");
    }
}
