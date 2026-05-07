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
}