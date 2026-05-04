using LolClientHelper.ViewModels;

namespace LolClientHelper.Views;

public partial class LogsView : ContentView
{
    public LogsView()
    {
        InitializeComponent();
    }

    public LogsView(LogsViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
}