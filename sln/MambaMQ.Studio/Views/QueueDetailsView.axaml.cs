namespace MambaMQ.Studio.Views;

public partial class QueueDetailsView : UserControl
{
    public QueueDetailsView()
    {
        InitializeComponent();
    }

    private async void UpdateQueue_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not QueueDetailsViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;

        UpdateQueueWindow window = new(viewModel.Queue);

        QueueOptions? options = await window.ShowDialog<QueueOptions?>(owner);

        if (options is null)
            return;

        await viewModel.UpdateQueue(options);
    }

    private async void DeleteQueue_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not QueueDetailsViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;

        ConfirmDeleteQueueWindow window = new(viewModel.Queue.Name);

        bool confirmed = await window.ShowDialog<bool>(owner);

        if (!confirmed)
            return;

        QueueOperationResult result = await viewModel.DeleteQueue();

        if (!result.IsSucceed)
            return;

        viewModel.GoBackCommand.Execute(null);
    }
}