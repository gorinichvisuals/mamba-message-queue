namespace MambaMQ.Studio.Views;

public partial class CreateMessageWindow : Window
{
    public CreateMessageWindow()
    {
        InitializeComponent();
        WindowChrome.Apply(this);

        Opened += (_, _) => MessageTextBox.Focus();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void PublishButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string? message = MessageTextBox.Text;

        if (string.IsNullOrWhiteSpace(message))
            return;

        Close(message);
    }
}