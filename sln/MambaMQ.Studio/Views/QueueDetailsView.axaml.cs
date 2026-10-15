namespace MambaMQ.Studio.Views;

public partial class QueueDetailsView : UserControl
{
    private bool _initialized;

    public QueueDetailsView()
    {
        InitializeComponent();

        Loaded += QueueDetailsView_Loaded;
    }

    private async void QueueDetailsView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (_initialized || DataContext is not QueueDetailsViewModel viewModel)
            return;

        _initialized = true;

        await viewModel.LoadMessagesAsync();
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

        ConfirmDeleteWindow window = new(
            "Delete queue?",
            $"Are you sure you want to delete queue \"{viewModel.Queue.Name}\"? This action cannot be undone.");

        bool confirmed = await window.ShowDialog<bool>(owner);

        if (!confirmed)
            return;

        QueueOperationResult result = await viewModel.DeleteQueue();

        if (!result.IsSucceed)
            return;

        viewModel.GoBackCommand.Execute(null);
    }

    private async void DeleteSelectedMessages_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not QueueDetailsViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;

        int selectedCount = viewModel.Messages.Count(message => message.IsSelected);

        if (selectedCount is 0)
            return;

        string message = selectedCount is 1
            ? "Are you sure you want to delete the selected message? This action cannot be undone."
            : $"Are you sure you want to delete the {selectedCount} selected messages? This action cannot be undone.";

        ConfirmDeleteWindow window = new("Delete messages?", message);

        bool confirmed = await window.ShowDialog<bool>(owner);

        if (confirmed)
            await viewModel.DeleteSelectedMessagesCommand.ExecuteAsync(null);
    }

    private async void CreateMessage_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not QueueDetailsViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;

        CreateMessageWindow window = new();

        string? message = await window.ShowDialog<string?>(owner);

        if (message is null)
            return;

        await viewModel.PublishMessage(message);
    }
    
    private async void MessageRow_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not MessageListItemModel item || TopLevel.GetTopLevel(this) is not Window owner)
            return;

        MessageDetailsWindow window = new(item.Message);

        await window.ShowDialog(owner);
    }
    
    private void MessageCheckbox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        e.Handled = true;
    }
    
    private void MessageRow_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
            border.Background = new SolidColorBrush(Color.Parse("#383838"));
    }

    private void MessageRow_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
            border.Background = Brushes.Transparent;
    }
}
