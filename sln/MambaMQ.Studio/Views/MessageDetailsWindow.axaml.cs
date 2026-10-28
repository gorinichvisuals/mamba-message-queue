namespace MambaMQ.Studio.Views;

public partial class MessageDetailsWindow : Window
{
    public MessageDetailsWindow(QueueMessageModel message)
    {
        InitializeComponent();
        WindowChrome.Apply(this);

        DataContext = message;
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}