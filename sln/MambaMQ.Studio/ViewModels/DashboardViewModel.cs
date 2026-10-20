namespace MambaMQ.Studio.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IMambaStudioClient _mambaClient;
    private readonly Action _onDisconnected;

    private string Host { get; }
    private int Port { get; }
    public string Username { get; }

    public string ConnectionInfo => $"{Host}:{Port}";

    private QueuesViewModel? _queuesViewModel;
    private ExchangesViewModel? _exchangesViewModel;

    [ObservableProperty]
    private ViewModelBase _currentViewModel = null!;

    public DashboardViewModel(
        IMambaStudioClient mambaClient,
        string host,
        int port,
        string username,
        Action onDisconnected)
    {
        _mambaClient = mambaClient;
        _onDisconnected = onDisconnected;
        Host = host;
        Port = port;
        Username = username;
        _exchangesViewModel = new ExchangesViewModel(mambaClient, ShowExchangeDetails);
        _currentViewModel = _exchangesViewModel;
    }

    [RelayCommand]
    private void ShowExchanges()
    {
        _exchangesViewModel ??= CurrentViewModel as ExchangesViewModel ?? new ExchangesViewModel(_mambaClient, ShowExchangeDetails);

        CurrentViewModel = _exchangesViewModel;
        _ = _exchangesViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void ShowQueues()
    {
        _queuesViewModel ??= new QueuesViewModel(_mambaClient, ShowQueueDetails);

        CurrentViewModel = _queuesViewModel;
    }
    
    private void ShowQueueDetails(QueueModel queue)
    {
        _queuesViewModel ??= new QueuesViewModel(_mambaClient, ShowQueueDetails);

        CurrentViewModel = new QueueDetailsViewModel(_mambaClient, queue, ShowQueues);
    }

    private void ShowExchangeDetails(ExchangeModel exchange)
    {
        CurrentViewModel = new ExchangeDetailsViewModel(_mambaClient, exchange, ShowExchanges, ShowQueueDetails);
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _mambaClient.DisconnectAsync();
        
        _onDisconnected();
    }
}
