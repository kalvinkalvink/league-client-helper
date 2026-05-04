using LolClientHelper.ViewModels;

namespace LolClientHelper.Views;

public partial class GameAutoView : ContentView
{
    public GameAutoView()
    {
        InitializeComponent();
    }

    public GameAutoView(GameAutoViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
}