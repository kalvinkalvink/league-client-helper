using LolClientHelper.ViewModels;

namespace LolClientHelper.Views;

public partial class LobbyView : ContentView
{
    public LobbyView()
    {
        InitializeComponent();
    }

    public LobbyView(LobbyViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
}