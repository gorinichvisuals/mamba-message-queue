namespace MambaMQ.Studio.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    private readonly IMambaStudioClient _mambaClient;
    private readonly IConnectionProfileService _profileService;

    public MainViewModel(IMambaStudioClient mambaClient, IConnectionProfileService profileService)
    {
        _mambaClient = mambaClient;
        _profileService = profileService;

        _currentViewModel = new LoginViewModel(_mambaClient, _profileService, OnLoginSucceeded);
    }

    private void OnLoginSucceeded(string host, int port, string username)
        => CurrentViewModel = new DashboardViewModel(_mambaClient, host, port, username, OnDisconnected);
    
    private void OnDisconnected()
        => CurrentViewModel = new LoginViewModel(_mambaClient, _profileService, OnLoginSucceeded);
}