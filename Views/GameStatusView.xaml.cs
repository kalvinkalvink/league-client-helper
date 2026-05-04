using LolClientHelper.ViewModels;

namespace LolClientHelper.Views;

public partial class GameStatusView : ContentView
{
    public GameStatusView()
    {
        InitializeComponent();
    }

    public GameStatusView(GameStatusViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
}