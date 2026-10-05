namespace MambaMQ.Studio.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    private readonly IMambaStudioClient _mambaClient;

    public MainViewModel(IMambaStudioClient mambaClient)
    {
        _mambaClient = mambaClient;

        _currentViewModel = new LoginViewModel(_mambaClient, OnLoginSucceeded);
    }

    private void OnLoginSucceeded(string host, int port, string username)
        => CurrentViewModel = new DashboardViewModel(_mambaClient, host, port, username, OnDisconnected);
    
    private void OnDisconnected()
        => CurrentViewModel = new LoginViewModel(_mambaClient, OnLoginSucceeded);
}