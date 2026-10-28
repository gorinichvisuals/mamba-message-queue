namespace MambaMQ.Studio.Views;

public partial class QueuesView : UserControl
{
    private int _selectedColumnIndex = -1;
    private QueueModel? _selectedQueue;

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
        if (DataContext is not QueuesViewModel viewModel ||
            sender is not Control control ||
            control.DataContext is not QueueModel queue)
            return;

        viewModel.OpenQueueCommand.Execute(queue);
    }

    private void QueuesGrid_OnCellPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || !int.TryParse(control.Tag?.ToString(), out int columnIndex))
            return;

        _selectedColumnIndex = columnIndex;
        _selectedQueue = control.DataContext as QueueModel;
        QueuesGrid.Focus();
    }

    private async void QueuesGrid_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || e.KeyModifiers != KeyModifiers.Control)
            return;

        if (_selectedQueue is not { } queue)
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

    private void QueueRow_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
            border.Background = new SolidColorBrush(Color.Parse("#383838"));
    }

    private void QueueRow_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
            border.Background = Brushes.Transparent;
    }
}