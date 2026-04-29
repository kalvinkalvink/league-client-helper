using LolClientHelper.Views;

namespace LolClientHelper.Services;

public sealed class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggingService _log;

    public WindowService(IServiceProvider serviceProvider, ILoggingService log)
    {
        _serviceProvider = serviceProvider;
        _log = log;
    }

    public void ShowSettingsWindow()
    {
        SettingsWindow.ShowSingleton(_serviceProvider, _log);
    }
}
