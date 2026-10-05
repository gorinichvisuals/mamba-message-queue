namespace MambaMQ.Studio.Views;

public partial class CreateQueueWindow : Window
{
    public CreateQueueWindow()
    {
        InitializeComponent();

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        DataContext = new CreateQueueViewModel();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void CreateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CreateQueueViewModel viewModel)
            return;

        if (string.IsNullOrWhiteSpace(viewModel.QueueName))
            return;

        Dictionary<string, QueuePermission> permissions =
            viewModel.Permissions.ToDictionary(
                x => x.ServiceName,
                x => x.Permissions);

        QueueOptions options = new()
        {
            QueueName = viewModel.QueueName.Trim(),
            IsDurable = viewModel.IsDurable,
            LoadBalancingAlgorithm = viewModel.LoadBalancingAlgorithm,
            Permissions = permissions,
            MessageRetention = new MessageRetentionOptions
            {
                Enabled = viewModel.MessageRetentionEnabled,
                RetentionPeriod = TimeSpan.FromMinutes(viewModel.RetentionPeriod)
            }
        };

        Close(options);
    }
}