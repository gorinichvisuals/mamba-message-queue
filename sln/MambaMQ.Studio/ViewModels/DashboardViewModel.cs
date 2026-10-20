namespace MambaMQ.Studio.ViewModels;

public partial class DashboardViewModel(
    IMambaStudioClient mambaClient,
    string host,
    int port,
    string username,
    Action onDisconnected) : ViewModelBase
{
    private string Host { get; } = host;
    private int Port { get; } = port;
    public string Username { get; } = username;

    public string ConnectionInfo => $"{Host}:{Port}";

    private QueuesViewModel? _queuesViewModel;
    private ExchangesViewModel? _exchangesViewModel;
    
    [ObservableProperty]
    private ViewModelBase _currentViewModel = new ExchangesViewModel(mambaClient);

    [RelayCommand]
    private void ShowExchanges()
    {
        _exchangesViewModel ??= CurrentViewModel as ExchangesViewModel ?? new ExchangesViewModel(mambaClient);

        CurrentViewModel = _exchangesViewModel;
    }

    [RelayCommand]
    private void ShowQueues()
    {
        _queuesViewModel ??= new QueuesViewModel(mambaClient, ShowQueueDetails);

        CurrentViewModel = _queuesViewModel;
    }
    
    private void ShowQueueDetails(QueueModel queue)
    {
        _queuesViewModel ??= new QueuesViewModel(mambaClient, ShowQueueDetails);

        CurrentViewModel = new QueueDetailsViewModel(mambaClient, queue, ShowQueues);
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await mambaClient.DisconnectAsync();
        
        onDisconnected();
    }
}
