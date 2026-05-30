using System.ComponentModel;
using LolClientHelper.ViewModels;
using LolClientHelper.Views;

namespace LolClientHelper;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly TabTemplateSelector _tabSelector;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        // Resolve the TabTemplateSelector registered as a XAML resource.
        _tabSelector = (TabTemplateSelector)Resources["TabSelector"];

        // Swap content immediately for the initial tab, then on every change.
        SwapTabContent();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // SelectedTab fires OnPropertyChanged(string.Empty) which passes "" or null name.
        // We react to both to be safe.
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(MainViewModel.SelectedTab))
            SwapTabContent();
    }

    private void SwapTabContent()
    {
        // SelectTemplate instantiates the view declared in the DataTemplate for the
        // current SelectedTab — previous tab's View is discarded (GC-eligible).
        // Do NOT set BindingContext here: the DataTemplate already binds the correct
        // sub-ViewModel (e.g. BindingContext="{Binding LobbyVM}") during CreateContent().
        // Overwriting it with MainViewModel would break all compiled bindings in each view.
        var template = _tabSelector.SelectTemplate(_viewModel, this);
        TabContentView.Content = template?.CreateContent() as View;
    }
}
