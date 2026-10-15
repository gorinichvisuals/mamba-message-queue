namespace MambaMQ.Studio.Views;

public partial class ConfirmDeleteQueueWindow : Window
{
    public ConfirmDeleteQueueWindow(string queueName)
    {
        InitializeComponent();

        MessageText.Text =
            $"Are you sure you want to delete queue \"{queueName}\"? " +
            "This action cannot be undone.";
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
        => Close(false);
    

    private void Confirm_Click(object? sender, RoutedEventArgs e)
        => Close(true);
}