namespace MambaMQ.Studio.Views;

public partial class SaveProfilePromptWindow : Window
{
    public SaveProfilePromptWindow()
    {
        InitializeComponent();
        WindowChrome.Apply(this);
    }

    private void ConnectWithoutSaving_Click(object? sender, RoutedEventArgs e)
        => Close(false);

    private void SaveAndConnect_Click(object? sender, RoutedEventArgs e)
        => Close(true);
}