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

    [ObservableProperty]
    private ViewModelBase _currentViewModel = new ExchangesViewModel();

    [RelayCommand]
    private void ShowExchanges()
    {
        CurrentViewModel = new ExchangesViewModel();
    }

    [RelayCommand]
    private void ShowQueues()
    {
        CurrentViewModel = new QueuesViewModel(mambaClient);
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await mambaClient.DisconnectAsync();
        
        onDisconnected();
    }
}