using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using LolClientHelper.Services;
using LolClientHelper.ViewModels;

namespace LolClientHelper.Views;

public partial class LogsView : ContentView
{
    private bool _autoScrollEnabled = true;
    private LogsViewModel? _currentViewModel;

    public LogsView()
    {
        InitializeComponent();
        // Auto-scroll enabled by default, set active color
        AutoScrollToggleButton.BackgroundColor = Color.FromArgb("#3B82F6");
    }

    public LogsView(LogsViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_currentViewModel != null)
        {
            _currentViewModel.LogEntries.CollectionChanged -= OnLogEntriesChanged;
        }

        if (BindingContext is LogsViewModel vm)
        {
            _currentViewModel = vm;
            vm.LogEntries.CollectionChanged += OnLogEntriesChanged;
        }
        else
        {
            _currentViewModel = null;
        }
    }

    private void OnLogEntriesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        if (!_autoScrollEnabled || BindingContext is not LogsViewModel vm)
            return;

        var items = vm.LogEntries;
        if (items.Count == 0)
            return;

        // Use dispatcher with delay to allow CollectionView layout to complete
        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(50);
            LogCollectionView.ScrollTo(items.Count - 1, position: ScrollToPosition.End, animate: false);
        });
    }

    private void OnMaxLogEntriesCompleted(object sender, EventArgs e)
    {
        UpdateMaxLogEntries();
    }

    private void OnMaxLogEntriesUnfocused(object sender, FocusEventArgs e)
    {
        UpdateMaxLogEntries();
    }

    private void UpdateMaxLogEntries()
    {
        if (BindingContext is not LogsViewModel vm)
            return;

        var text = MaxLogEntriesEntry.Text;
        if (int.TryParse(text, out int result) && result > 0)
        {
            vm.MaxLogEntries = result;
        }
        else
        {
            MaxLogEntriesEntry.Text = vm.MaxLogEntries.ToString();
        }
    }

    private void OnToggleAutoScrollClicked(object? sender, EventArgs e)
    {
        _autoScrollEnabled = !_autoScrollEnabled;
        if (sender is Button btn)
        {
            btn.Text = _autoScrollEnabled ? "↓ Auto" : "↓";
            // Change color: blue when active, dark gray when inactive
            btn.BackgroundColor = _autoScrollEnabled ? Color.FromArgb("#3B82F6") : Color.FromArgb("#404040");
        }
        if (_autoScrollEnabled)
        {
            ScrollToBottom();
        }
    }

    private void OnLogCollectionViewLoaded(object? sender, EventArgs e)
    {
        if (!_autoScrollEnabled || BindingContext is not LogsViewModel vm)
            return;

        if (vm.LogEntries.Count == 0)
            return;

        ScrollToBottom();
    }
}