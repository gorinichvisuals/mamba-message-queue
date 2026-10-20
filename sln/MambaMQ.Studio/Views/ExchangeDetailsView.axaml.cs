namespace MambaMQ.Studio.Views;

public partial class ExchangeDetailsView : UserControl
{
    private bool _initialized;

    public ExchangeDetailsView()
    {
        InitializeComponent();
        
        Loaded += async (_, _) =>
        {
            if (_initialized || DataContext is not ExchangeDetailsViewModel viewModel)
                return;
            
            _initialized = true;
            
            await viewModel.LoadAsync();
        };
    }

    private async void UpdateExchange_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ExchangeDetailsViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;
        
        UpdateExchangeWindow window = new(viewModel.Exchange);
        ExchangeOptions? options = await window.ShowDialog<ExchangeOptions?>(owner);
        
        if (options is not null)
            await viewModel.UpdateExchange(options);
    }

    private async void DeleteExchange_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ExchangeDetailsViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;
        
        ConfirmDeleteWindow window = new("Delete exchange?", $"Are you sure you want to delete exchange \"{viewModel.Exchange.Name}\"? This action cannot be undone.");
        
        if (await window.ShowDialog<bool>(owner) && await viewModel.DeleteExchange())
            viewModel.GoBackCommand.Execute(null);
    }

    private async void BindQueue_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ExchangeDetailsViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;
        
        if (viewModel.AvailableQueueNames.Count == 0)
        {
            viewModel.ErrorMessage = "Create a queue before binding it to this exchange.";
            
            return;
        }
        
        BindQueueToExchangeWindow window = new(viewModel.AvailableQueueNames);
        ExchangeBindingModel? binding = await window.ShowDialog<ExchangeBindingModel?>(owner);
        
        if (binding is not null)
            await viewModel.BindQueueToExchange(binding);
    }

    private async void UnbindQueue_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ExchangeBindingModel binding } && DataContext is ExchangeDetailsViewModel viewModel)
            await viewModel.UnbindQueueFromExchange(binding);
    }

    private async void OpenQueue_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string queueName } && DataContext is ExchangeDetailsViewModel viewModel)
            await viewModel.OpenQueue(queueName);
    }

    private async void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ExchangeDetailsViewModel viewModel)
            await viewModel.LoadAsync();
    }

    private async void SendMessage_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ExchangeDetailsViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;
        
        PublishToExchangeWindow window = new();
        PublishToExchangeOptions? options = await window.ShowDialog<PublishToExchangeOptions?>(owner);
        
        if (options is not null)
            await viewModel.PublishMessage(options.RoutingKey, options.Message);
    }
}