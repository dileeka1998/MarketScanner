namespace MarketScanner.Views.Dialogs;

public partial class InputDialog : ContentPage
{
    private TaskCompletionSource<string?> _tcs;

    public InputDialog(string defaultValue = "")
    {
        InitializeComponent();
        InputEntry.Text = defaultValue;
        _tcs = new TaskCompletionSource<string?>();
    }

    public Task<string?> GetInputAsync()
    {
        return _tcs.Task;
    }

    private async void OnOkClicked(object sender, EventArgs e)
    {
        _tcs.SetResult(InputEntry.Text);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.SetResult(null);
        await Navigation.PopModalAsync();
    }
}

