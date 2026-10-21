namespace MambaMQ.Studio.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IMambaStudioClient _mambaClient;
    private readonly Action _onDisconnected;
    private readonly string _password;
    private readonly CancellationTokenSource _connectionMonitorCts = new();
    private readonly Task _connectionMonitorTask;

    private string Host { get; }
    private int Port { get; }
    public string Username { get; }

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private bool _isReconnecting;

    public bool IsConnectionLost => !IsConnected;
    public string ConnectionStatusText => IsConnected
        ? "Connected"
        : IsReconnecting ? "Connection lost. Reconnecting…" : "Connection lost";
    
    public IBrush ConnectionLampBrush => IsConnected
        ? new SolidColorBrush(Color.Parse("#42D392"))
        : new SolidColorBrush(Color.Parse("#F05B63"));

    public string ConnectionInfo => $"{Host}:{Port}";

    private QueuesViewModel? _queuesViewModel;
    private ExchangesViewModel? _exchangesViewModel;

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    public DashboardViewModel(
        IMambaStudioClient mambaClient,
        string host,
        int port,
        string username,
        string password,
        Action onDisconnected)
    {
        _mambaClient = mambaClient;
        _onDisconnected = onDisconnected;
        _password = password;
        Host = host;
        Port = port;
        Username = username;
        _exchangesViewModel = new ExchangesViewModel(mambaClient, ShowExchangeDetails);
        _currentViewModel = _exchangesViewModel;
        _connectionMonitorTask = MonitorConnectionAsync(_connectionMonitorCts.Token);
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
        await _connectionMonitorCts.CancelAsync();
        
        await _connectionMonitorTask;
        await _mambaClient.DisconnectAsync();
        
        _onDisconnected();
    }

    private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(5));

        try
        {
            while (true)
            {
                if (IsConnected)
                {
                    try
                    {
                        using CancellationTokenSource pingTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        
                        pingTimeout.CancelAfter(TimeSpan.FromSeconds(8));
                        
                        await _mambaClient.PingAsync(pingTimeout.Token);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        await UpdateConnectionStatusAsync(false, true);
                    }
                }

                if (IsConnectionLost)
                {
                    try
                    {
                        await _mambaClient.DisconnectAsync(cancellationToken);

                        using CancellationTokenSource reconnectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        
                        reconnectTimeout.CancelAfter(TimeSpan.FromSeconds(8));
                        
                        await _mambaClient.ConnectAsync(Host, Port, Username, _password, reconnectTimeout.Token);

                        await UpdateConnectionStatusAsync(true, false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        await UpdateConnectionStatusAsync(false, true);
                    }
                }

                if (!await timer.WaitForNextTickAsync(cancellationToken))
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task UpdateConnectionStatusAsync(bool isConnected, bool isReconnecting)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsConnected = isConnected;
            IsReconnecting = isReconnecting;
            OnPropertyChanged(nameof(IsConnectionLost));
            OnPropertyChanged(nameof(ConnectionStatusText));
            OnPropertyChanged(nameof(ConnectionLampBrush));
        });
    }
}