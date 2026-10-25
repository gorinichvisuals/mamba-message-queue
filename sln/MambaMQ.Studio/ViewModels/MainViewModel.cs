namespace MambaMQ.Studio.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IMambaStudioClientFactory _clientFactory;
    private readonly IConnectionProfileService _profileService;

    public ObservableCollection<DashboardViewModel> Tabs { get; } = [];

    [ObservableProperty]
    private DashboardViewModel? _activeDashboard;

    [ObservableProperty]
    private LoginViewModel _loginViewModel = null!;

    [ObservableProperty]
    private bool _isLoginOverlayOpen;

    public bool HasActiveDashboard => ActiveDashboard is not null;

    public MainViewModel(IMambaStudioClientFactory clientFactory, IConnectionProfileService profileService)
    {
        _clientFactory = clientFactory;
        _profileService = profileService;
        ShowLoginOverlay();
    }

    [RelayCommand]
    private void ConnectTo() => ShowLoginOverlay();

    private void SelectTab(DashboardViewModel? dashboard)
    {
        if (dashboard is null || !Tabs.Contains(dashboard))
            return;

        if (ActiveDashboard is not null)
            ActiveDashboard.IsActive = false;

        ActiveDashboard = dashboard;
        dashboard.IsActive = true;
        OnPropertyChanged(nameof(HasActiveDashboard));
    }

    private bool ConnectionExists(string host, int port, string username)
        => Tabs.Any(tab =>
            string.Equals(tab.ConnectionInfo, $"{host}:{port}", StringComparison.OrdinalIgnoreCase)
            && string.Equals(tab.Username.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase));

    private void OnLoginSucceeded(string host, int port, string username, string password, IMambaStudioClient client)
    {
        DashboardViewModel dashboard = new(client, host, port, username, password, OnDisconnected, SelectTab);
        Tabs.Add(dashboard);
        SelectTab(dashboard);
        IsLoginOverlayOpen = false;
    }

    private void OnDisconnected(DashboardViewModel dashboard)
    {
        bool wasActive = ReferenceEquals(ActiveDashboard, dashboard);
        Tabs.Remove(dashboard);
        dashboard.IsActive = false;

        if (wasActive)
        {
            ActiveDashboard = Tabs.LastOrDefault();
            if (ActiveDashboard is not null)
                ActiveDashboard.IsActive = true;
        }

        OnPropertyChanged(nameof(HasActiveDashboard));

        if (Tabs.Count == 0)
            ShowLoginOverlay();
    }

    private void ShowLoginOverlay()
    {
        IMambaStudioClient client = _clientFactory.Create();

        LoginViewModel = new LoginViewModel(
            client,
            _profileService,
            ConnectionExists,
            OnLoginSucceeded,
            () => IsLoginOverlayOpen = false,
            Tabs.Count > 0);
        IsLoginOverlayOpen = true;
    }
}