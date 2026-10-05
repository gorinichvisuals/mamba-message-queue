namespace MambaMQ.Studio.Views;

public partial class QueuesView : UserControl
{
    private int _selectedColumnIndex = -1;

    public QueuesView()
    {
        InitializeComponent();

        QueuesGrid.AddHandler(KeyDownEvent, QueuesGrid_OnKeyDown, RoutingStrategies.Tunnel);

        Loaded += async (_, _) =>
        {
            if (DataContext is QueuesViewModel viewModel)
                await viewModel.LoadCommand.ExecuteAsync(null);
        };
    }

    private async void CreateQueueButton_OnClick(object? sender, RoutedEventArgs e)
    {
        CreateQueueWindow window = new();

        Window? owner = TopLevel.GetTopLevel(this) as Window;

        if (owner is null)
            return;

        QueueOptions? options =
            await window.ShowDialog<QueueOptions?>(owner);

        if (options is null)
            return;

        if (DataContext is QueuesViewModel viewModel)
        {
            await viewModel.CreateQueueCommand.ExecuteAsync(options);
        }
    }
    
    private async void UpdateQueueButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not QueueModel queue)
            return;

        UpdateQueueWindow window = new(queue);

        Window? owner = TopLevel.GetTopLevel(this) as Window;

        if (owner is null)
            return;

        QueueOptions? options = await window.ShowDialog<QueueOptions?>(owner);

        if (options is null)
            return;

        if (DataContext is QueuesViewModel viewModel)
            await viewModel.UpdateQueue(queue, options);
    }

    private void QueuesGrid_OnCurrentCellChanged(object? sender, EventArgs e)
    {
        _selectedColumnIndex = QueuesGrid.CurrentColumn.DisplayIndex;
    }

    private async void QueuesGrid_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.C ||
            e.KeyModifiers != KeyModifiers.Control)
        {
            return;
        }

        if (QueuesGrid.SelectedItem is not QueueModel queue)
            return;

        string? value = _selectedColumnIndex switch
        {
            0 => queue.Id.ToString(),
            1 => queue.Name,
            2 => queue.IsDurable ? "✓" : "✕",
            3 => queue.MessageRetentionEnabled ? "✓" : "✕",
            4 => queue.MessageRetentionPeriod.ToString(),
            5 => queue.LoadBalancingAlgorithm.ToString(),
            6 => queue.CreatedAt.ToString(),
            7 => queue.AvailableMessageCount.ToString(),
            8 => queue.InFlightMessageCount.ToString(),
            9 => queue.PermissionsDisplay,
            _ => null
        };

        if (value is null)
            return;

        TopLevel? topLevel = TopLevel.GetTopLevel(QueuesGrid);

        if (topLevel?.Clipboard is null)
            return;

        await topLevel.Clipboard.SetTextAsync(value);

        e.Handled = true;
    }
}