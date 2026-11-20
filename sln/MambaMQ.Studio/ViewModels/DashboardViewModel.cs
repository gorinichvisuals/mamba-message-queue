namespace MambaMQ.Studio.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IMambaStudioClient _mambaClient;
    private readonly Action<DashboardViewModel> _onDisconnected;
    private readonly Action<DashboardViewModel> _onSelected;
    private readonly string _password;
    private readonly CancellationTokenSource _connectionMonitorCts = new();
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private readonly Task _connectionMonitorTask;
    private CancellationTokenSource? _statisticsRefreshCts;

    private string Host { get; }
    private int Port { get; }
    public string ProfileName { get; }
    public string Username { get; }

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isReconnecting;

    [ObservableProperty]
    private bool _isNavigationLoading;

    [ObservableProperty]
    private string? _navigationErrorMessage;

    public bool IsConnectionLost => !IsConnected;
    public bool HasNavigationError => !string.IsNullOrWhiteSpace(NavigationErrorMessage);
    public bool IsNavigationOverlayVisible => IsNavigationLoading || HasNavigationError;
    public string NavigationOverlayText => HasNavigationError
        ? NavigationErrorMessage!
        : "Waiting for the connection to recover…";

    partial void OnIsNavigationLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNavigationOverlayVisible));
        OnPropertyChanged(nameof(NavigationOverlayText));
    }

    partial void OnNavigationErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasNavigationError));
        OnPropertyChanged(nameof(IsNavigationOverlayVisible));
        OnPropertyChanged(nameof(NavigationOverlayText));
    }

    public string ConnectionStatusText => IsConnected
        ? "Connected"
        : IsReconnecting ? "Connection lost. Reconnecting…" : "Connection lost";

    public IBrush ConnectionLampBrush => IsConnected
        ? new SolidColorBrush(Color.Parse("#42D392"))
        : new SolidColorBrush(Color.Parse("#F05B63"));

    public string ConnectionInfo => $"{Host}:{Port}";
    public string TabTitle => string.Equals(ProfileName, ConnectionInfo, StringComparison.OrdinalIgnoreCase)
        ? ConnectionInfo
        : ProfileName + " : " + ConnectionInfo;
    public IBrush TabBackgroundBrush => IsActive
        ? new SolidColorBrush(Color.Parse("#55475E7A"))
        : new SolidColorBrush(Color.Parse("#201A202B"));

    partial void OnIsActiveChanged(bool value)
        => OnPropertyChanged(nameof(TabBackgroundBrush));

    private QueuesViewModel? _queuesViewModel;
    private ExchangesViewModel? _exchangesViewModel;

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    public DashboardViewModel(
        IMambaStudioClient mambaClient,
        string host,
        int port,
        string profileName,
        string username,
        string password,
        Action<DashboardViewModel> onDisconnected,
        Action<DashboardViewModel> onSelected)
    {
        _mambaClient = mambaClient;
        _onDisconnected = onDisconnected;
        _onSelected = onSelected;
        _password = password;
        Host = host;
        Port = port;
        ProfileName = string.IsNullOrWhiteSpace(profileName) ? host + ":" + port : profileName;
        Username = username;
        _exchangesViewModel = new ExchangesViewModel(mambaClient, ShowExchangeDetails);
        _currentViewModel = _exchangesViewModel;
        _connectionMonitorTask = MonitorConnectionAsync(_connectionMonitorCts.Token);
    }

    [RelayCommand]
    private void ShowExchanges() => _ = NavigateAsync(async () =>
    {
        StopStatisticsRefresh();
        _exchangesViewModel ??= CurrentViewModel as ExchangesViewModel
            ?? new ExchangesViewModel(_mambaClient, ShowExchangeDetails);
        CurrentViewModel = _exchangesViewModel;
        await _exchangesViewModel.LoadCommand.ExecuteAsync(null);
    });

    [RelayCommand]
    private void Select() => _onSelected(this);

    [RelayCommand]
    private void ShowQueues() => _ = NavigateAsync(async () =>
    {
        StopStatisticsRefresh();
        _queuesViewModel ??= new QueuesViewModel(_mambaClient, ShowQueueDetails);
        CurrentViewModel = _queuesViewModel;
        await _queuesViewModel.LoadCommand.ExecuteAsync(null);
        StartStatisticsRefresh(_queuesViewModel);
    });

    private void ShowQueueDetails(QueueModel queue) => _ = NavigateAsync(async () =>
    {
        StopStatisticsRefresh();
        _queuesViewModel ??= new QueuesViewModel(_mambaClient, ShowQueueDetails);
        QueueDetailsViewModel viewModel = new(_mambaClient, queue, ShowQueues);
        CurrentViewModel = viewModel;
        await viewModel.LoadMessagesAsync();
        StartStatisticsRefresh(viewModel);
    });

    private void ShowExchangeDetails(ExchangeModel exchange) => _ = NavigateAsync(async () =>
    {
        ExchangeDetailsViewModel viewModel = new(_mambaClient, exchange, ShowExchanges, ShowQueueDetails);
        CurrentViewModel = viewModel;
        await viewModel.LoadAsync();
    });

    [RelayCommand]
    public async Task DisconnectAsync()
    {
        StopStatisticsRefresh();
        await _connectionMonitorCts.CancelAsync();

        await _connectionMonitorTask;
        await _mambaClient.DisconnectAsync();

        _onDisconnected(this);
    }

    private void StartStatisticsRefresh(ViewModelBase viewModel)
    {
        StopStatisticsRefresh();
        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_connectionMonitorCts.Token);
        _statisticsRefreshCts = cts;
        _ = RefreshStatisticsPeriodicallyAsync(viewModel, cts);
    }

    private void StopStatisticsRefresh()
    {
        CancellationTokenSource? cts = _statisticsRefreshCts;
        _statisticsRefreshCts = null;
        if (cts is not null)
            cts.Cancel();
    }

    private async Task RefreshStatisticsPeriodicallyAsync(ViewModelBase viewModel, CancellationTokenSource refreshCts)
    {
        CancellationToken cancellationToken = refreshCts.Token;
        try
        {
            Task statisticsTask = RefreshQueueStatisticsPeriodicallyAsync(viewModel, cancellationToken);
            Task messagesTask = viewModel is QueueDetailsViewModel queueDetailsViewModel
                ? RefreshMessagesPeriodicallyAsync(queueDetailsViewModel, cancellationToken)
                : Task.CompletedTask;
            Task ratesTask = viewModel is QueueDetailsViewModel ratesViewModel
                ? RefreshQueueRatesPeriodicallyAsync(ratesViewModel, cancellationToken)
                : Task.CompletedTask;

            await Task.WhenAll(statisticsTask, messagesTask, ratesTask);
        }
        finally
        {
            if (ReferenceEquals(_statisticsRefreshCts, refreshCts))
                _statisticsRefreshCts = null;
            refreshCts.Dispose();
        }
    }

    private async Task RefreshQueueStatisticsPeriodicallyAsync(ViewModelBase viewModel, CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(500));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!IsConnected)
                    continue;

                try
                {
                    if (viewModel is QueuesViewModel queuesViewModel)
                        await queuesViewModel.RefreshStatisticsAsync(cancellationToken);
                    else if (viewModel is QueueDetailsViewModel queueDetailsViewModel)
                        await queueDetailsViewModel.RefreshStatisticsAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    // Keep the timer alive; the next tick can recover after a transient request failure.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
        }
    }

    private async Task RefreshMessagesPeriodicallyAsync(QueueDetailsViewModel viewModel, CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(2));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!IsConnected)
                    continue;

                try
                {
                    await viewModel.RefreshMessagesQuietlyAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    // Keep the timer alive; the next tick can recover after a transient request failure.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
        }
    }

    private async Task RefreshQueueRatesPeriodicallyAsync(QueueDetailsViewModel viewModel, CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!IsConnected)
                    continue;

                try
                {
                    await viewModel.RefreshRatesAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    // Keep the timer alive; the next tick can recover after a transient request failure.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
        }
    }

    private async Task NavigateAsync(Func<Task> navigation)
    {
        if (!await _navigationLock.WaitAsync(0))
            return;

        try
        {
            NavigationErrorMessage = null;
            OnPropertyChanged(nameof(HasNavigationError));
            OnPropertyChanged(nameof(IsNavigationOverlayVisible));

            bool showOverlay = IsConnectionLost;
            if (showOverlay)
                IsNavigationLoading = true;

            while (IsConnectionLost)
            {
                if (!await TryReconnectAsync(_connectionMonitorCts.Token))
                    await Task.Delay(TimeSpan.FromSeconds(5), _connectionMonitorCts.Token);
            }

            await navigation();
        }
        catch (OperationCanceledException) when (_connectionMonitorCts.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            NavigationErrorMessage = $"Could not open the page: {exception.Message}";
            OnPropertyChanged(nameof(HasNavigationError));
            OnPropertyChanged(nameof(NavigationOverlayText));
        }
        finally
        {
            IsNavigationLoading = false;
            OnPropertyChanged(nameof(IsNavigationOverlayVisible));
            _navigationLock.Release();
        }
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
                    await TryReconnectAsync(cancellationToken);

                if (!await timer.WaitForNextTickAsync(cancellationToken))
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task<bool> TryReconnectAsync(CancellationToken cancellationToken)
    {
        await _reconnectLock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
                return true;

            await UpdateConnectionStatusAsync(false, true);
            try
            {
                await _mambaClient.DisconnectAsync(cancellationToken);

                using CancellationTokenSource reconnectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
               
                reconnectTimeout.CancelAfter(TimeSpan.FromSeconds(8));
                
                await _mambaClient.ConnectAsync(Host, Port, Username, _password, reconnectTimeout.Token);

                await UpdateConnectionStatusAsync(true, false);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                await UpdateConnectionStatusAsync(false, true);
                return false;
            }
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    private async Task UpdateConnectionStatusAsync(bool isConnected, bool isReconnecting)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsConnected = isConnected;
            IsReconnecting = isReconnecting;
            OnPropertyChanged(nameof(IsConnectionLost));
            OnPropertyChanged(nameof(ConnectionStatusText));
            OnPropertyChanged(nameof(ConnectionLampBrush));
        });
    }
}