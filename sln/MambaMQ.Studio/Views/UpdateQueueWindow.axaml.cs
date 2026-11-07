namespace MambaMQ.Studio.Views;

public partial class UpdateQueueWindow : Window
{
    public UpdateQueueWindow(QueueModel queue)
    {
        InitializeComponent();
        WindowChrome.Apply(this);

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        DataContext = new UpdateQueueViewModel(queue);
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
    
    private void RemovePermissionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ServicePermissionModel permission)
            return;

        if (DataContext is UpdateQueueViewModel viewModel)
            viewModel.RemovePermission(permission);
    }
    
    private void UpdateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UpdateQueueViewModel viewModel)
            return;

        if (string.IsNullOrWhiteSpace(viewModel.QueueName))
            return;

        Dictionary<string, QueuePermission> permissions =
            viewModel.Permissions.ToDictionary(
                x => x.ServiceName,
                x => x.Permissions);

        QueueUpdateOptions options = new()
        {
            QueueName = viewModel.QueueName.Trim(),
            LoadBalancingAlgorithm = viewModel.LoadBalancingAlgorithm,
            Permissions = permissions,
            MessageRetentionPeriod = TimeSpan.FromMinutes(viewModel.RetentionPeriod)
        };

        Close(options);
    }
}
