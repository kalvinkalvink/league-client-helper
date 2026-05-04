using LolClientHelper.ViewModels;

namespace LolClientHelper.Views;

public partial class SettingsView : ContentView
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public SettingsView(SettingsViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
}