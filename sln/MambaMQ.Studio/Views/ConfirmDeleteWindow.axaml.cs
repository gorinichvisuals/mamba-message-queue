namespace MambaMQ.Studio.Views;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow(string title, string message)
    {
        InitializeComponent();
        WindowChrome.Apply(this);

        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
        => Close(false);

    private void Confirm_Click(object? sender, RoutedEventArgs e)
        => Close(true);
}