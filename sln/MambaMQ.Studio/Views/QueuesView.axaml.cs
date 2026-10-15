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

        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        QueueOptions? options = await window.ShowDialog<QueueOptions?>(owner);

        if (options is null)
            return;

        if (DataContext is QueuesViewModel viewModel)
            await viewModel.CreateQueueCommand.ExecuteAsync(options);
    }

    private void QueuesGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not QueuesViewModel viewModel)
            return;

        if (QueuesGrid.SelectedItem is not QueueModel queue)
            return;

        viewModel.OpenQueueCommand.Execute(queue);
    }

    private void QueuesGrid_OnCurrentCellChanged(object? sender, EventArgs e)
    {
        if (QueuesGrid.CurrentColumn is not null)
            _selectedColumnIndex = QueuesGrid.CurrentColumn.DisplayIndex;
    }

    private async void QueuesGrid_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || e.KeyModifiers != KeyModifiers.Control)
            return;

        if (QueuesGrid.SelectedItem is not QueueModel queue)
            return;

        string? value = _selectedColumnIndex switch
        {
            0 => queue.Name,
            1 => queue.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            2 => queue.AvailableMessageCount.ToString(),
            3 => queue.InFlightMessageCount.ToString(),
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