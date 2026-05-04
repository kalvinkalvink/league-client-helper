using LolClientHelper.ViewModels;

namespace LolClientHelper.Views;

public partial class MainPageTabView : ContentView
{
    public MainPageTabView()
    {
        InitializeComponent();
    }

    public MainPageTabView(MainPageTabViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
}