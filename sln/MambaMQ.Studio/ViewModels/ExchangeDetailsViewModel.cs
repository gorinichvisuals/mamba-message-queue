namespace MambaMQ.Studio.ViewModels;

public partial class ExchangeDetailsViewModel(
    IMambaStudioClient mambaClient,
    ExchangeModel exchange,
    Action onBack,
    Action<QueueModel> onQueueSelected) : ViewModelBase
{
    [ObservableProperty]
    private ExchangeModel _exchange = exchange;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ObservableCollection<string> _availableQueueNames = [];

    public int BoundQueueCount => Exchange.BoundQueueCount;
    public int BindingCount => Exchange.Bindings.Count;
    public bool HasBindings => Exchange.Bindings.Count > 0;
    public bool HasNoBindings => !HasBindings;

    public string PermissionsSummary => Exchange.Permissions.Count == 0
        ? "None"
        : string.Join(", ", Exchange.Permissions.Select(permission => $"{permission.Key}: {permission.Value}"));

    partial void OnExchangeChanged(ExchangeModel value)
    {
        OnPropertyChanged(nameof(BoundQueueCount));
        OnPropertyChanged(nameof(BindingCount));
        OnPropertyChanged(nameof(HasBindings));
        OnPropertyChanged(nameof(HasNoBindings));
        OnPropertyChanged(nameof(PermissionsSummary));
    }

    [RelayCommand]
    private void GoBack() => onBack();

    public async Task LoadAsync()
    {
        try
        {
            ErrorMessage = null;
            
            GetExchangesResponse exchanges = await mambaClient.GetExchangesAsync();
            
            ExchangeModel? updated = exchanges.Exchanges.FirstOrDefault(item => item.Id == Exchange.Id);
            
            if (updated is null)
            {
                ErrorMessage = $"Exchange '{Exchange.Name}' no longer exists.";
                
                return;
            }

            Exchange = updated;

            GetQueuesResponse queues = await mambaClient.GetQueuesAsync();
            
            AvailableQueueNames = new ObservableCollection<string>(queues.Queues.Select(queue => queue.Name));
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    public async Task<bool> BindQueueToExchange(ExchangeBindingModel binding)
    {
        try
        {
            ErrorMessage = null;
            
            CommandResponse response = await mambaClient.BindQueueToExchangeAsync(Exchange.Name, binding.QueueName, binding.RoutingKey);
            
            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;
                
                return false;
            }

            await LoadAsync();
            
            return true;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            
            return false;
        }
    }

    public async Task UnbindQueueFromExchange(ExchangeBindingModel binding)
    {
        try
        {
            ErrorMessage = null;
            
            CommandResponse response = await mambaClient.UnbindQueueFromExchangeAsync(Exchange.Name, binding.QueueName, binding.RoutingKey);
            
            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;
                
                return;
            }

            await LoadAsync();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    public async Task OpenQueue(string queueName)
    {
        try
        {
            ErrorMessage = null;
            
            GetQueuesResponse response = await mambaClient.GetQueuesAsync();
            
            QueueModel? queue = response.Queues.FirstOrDefault(item => item.Name == queueName);
            
            if (queue is null)
            {
                ErrorMessage = $"Queue '{queueName}' no longer exists.";
                
                return;
            }

            onQueueSelected(queue);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    public async Task<bool> UpdateExchange(ExchangeOptions options)
    {
        try
        {
            ErrorMessage = null;
            
            CommandResponse response = await mambaClient.UpdateExchangeAsync(Exchange.Id, options);
            
            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;
                
                return false;
            }

            await LoadAsync();
            
            return true;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            
            return false;
        }
    }

    public async Task<bool> DeleteExchange()
    {
        try
        {
            ErrorMessage = null;
            
            CommandResponse response = await mambaClient.DeleteExchangeAsync(Exchange.Id);
            
            if (response.IsSucceed)
                return true;

            ErrorMessage = response.ErrorMessage;
            return false;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            return false;
        }
    }

    public async Task PublishMessage(string routingKey, string message)
    {
        try
        {
            ErrorMessage = null;
            
            CommandResponse response = await mambaClient.PublishMessageToExchangeAsync(
                Exchange.Name,
                routingKey,
                Encoding.UTF8.GetBytes(message));

            if (!response.IsSucceed)
                ErrorMessage = response.ErrorMessage;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }
}