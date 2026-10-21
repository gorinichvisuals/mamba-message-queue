namespace MambaMQ.Studio.Views;

public partial class SaveProfilePromptWindow : Window
{
    public SaveProfilePromptWindow()
    {
        InitializeComponent();
    }

    private void ConnectWithoutSaving_Click(object? sender, RoutedEventArgs e)
        => Close(false);

    private void SaveAndConnect_Click(object? sender, RoutedEventArgs e)
        => Close(true);
}