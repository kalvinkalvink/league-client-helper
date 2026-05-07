using LolClientHelper.ViewModels;

namespace LolClientHelper.Views;

/// <summary>
/// Selects the DataTemplate for the active tab based on MainViewModel.SelectedTab.
/// This enables lazy instantiation — only the currently visible tab view is created,
/// replacing the previous pattern of rendering all 8 views simultaneously.
/// Called from MainPage.xaml.cs whenever SelectedTab changes.
/// </summary>
public sealed class TabTemplateSelector : DataTemplateSelector
{
    public DataTemplate? GameAutoTemplate     { get; set; }
    public DataTemplate? MainPageTemplate     { get; set; }
    public DataTemplate? LobbyTemplate        { get; set; }
    public DataTemplate? GameStatusTemplate   { get; set; }
    public DataTemplate? ChampSelectTemplate  { get; set; }
    public DataTemplate? EndOfGameTemplate    { get; set; }
    public DataTemplate? LogsTemplate         { get; set; }
    public DataTemplate? SettingsTemplate     { get; set; }

    /// <summary>
    /// Public wrapper so MainPage.xaml.cs can call it directly without reflection.
    /// </summary>
    public DataTemplate? SelectTemplate(MainViewModel vm, BindableObject container)
        => OnSelectTemplate(vm, container);

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
    {
        if (item is not MainViewModel vm)
            return GameAutoTemplate;

        return vm.SelectedTab switch
        {
            "GameAuto"    => GameAutoTemplate,
            "MainPage"    => MainPageTemplate,
            "Lobby"       => LobbyTemplate,
            "GameStatus"  => GameStatusTemplate,
            "ChampSelect" => ChampSelectTemplate,
            "EndOfGame"   => EndOfGameTemplate,
            "Logs"        => LogsTemplate,
            "Settings"    => SettingsTemplate,
            _             => GameAutoTemplate
        };
    }
}
