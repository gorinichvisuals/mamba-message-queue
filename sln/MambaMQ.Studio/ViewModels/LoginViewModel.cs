namespace MambaMQ.Studio.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IMambaStudioClient _mambaClient;
    private readonly IConnectionProfileService _profileService;
    private readonly Action<string, int, string, string> _onLoginSucceeded;
    private bool _isApplyingProfile;
    private Avalonia.Threading.DispatcherTimer? _connectionStatusTimer;
    private int _connectionDots;

    public ObservableCollection<ConnectionProfile> SavedProfiles { get; } = [];

    public LoginViewModel(
        IMambaStudioClient mambaClient,
        IConnectionProfileService profileService,
        Action<string, int, string, string> onLoginSucceeded)
    {
        _mambaClient = mambaClient;
        _profileService = profileService;
        _onLoginSucceeded = onLoginSucceeded;

        _ = LoadProfilesAsync();
    }

    [ObservableProperty]
    public partial ConnectionProfile? SelectedProfile { get; set; }

    [ObservableProperty]
    public partial string Host { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCurrentProfileSaved { get; set; }

    [ObservableProperty]
    public partial bool IsConnecting { get; set; }

    [ObservableProperty]
    private partial bool IsPasswordVisible { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public char? PasswordChar => IsPasswordVisible ? null : '*';
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasProfiles => SavedProfiles.Count > 0;
    public bool ShowEmptyProfilesMessage => !HasProfiles;
    public bool IsNotConnecting => !IsConnecting;
    public string ConnectButtonText => IsConnecting ? "Connecting…" : "Connect";
    public string ConnectionStatusDots { get; private set; } = ".";

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private Task SaveProfileAsync() => SaveProfileForConnectionAsync();

    public async Task<bool> SaveProfileForConnectionAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Enter a host, username, and password before saving the profile.";
            
            return false;
        }

        ConnectionProfile profile = new()
        {
            Id = Guid.CreateVersion7().ToString("N"),
            Name = Host.Trim(),
            Host = Host.Trim(),
            Username = Username.Trim(),
            Password = Password
        };

        try
        {
            await _profileService.SaveProfileAsync(profile);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not save the profile: {exception.Message}";
            
            return false;
        }

        SavedProfiles.Add(profile);
        SelectedProfile = profile;
        IsCurrentProfileSaved = true;
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(ShowEmptyProfilesMessage));
        
        return true;
    }

    [RelayCommand]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile is null)
            return;

        ConnectionProfile profile = SelectedProfile;
        
        await _profileService.DeleteProfileAsync(profile);
        
        SavedProfiles.Remove(profile);
        SelectedProfile = null;
        Host = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        ErrorMessage = null;
        
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(ShowEmptyProfilesMessage));
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        if (IsConnecting)
            return;

        ErrorMessage = null;
        string[] parts = Host.Split(':', 2);

        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || !int.TryParse(parts[1], out int port) || port is < 1 or > 65535)
        {
            ErrorMessage = "Invalid host or port.";
            
            return;
        }

        IsConnecting = true;

        try
        {
            await _mambaClient.ConnectAsync(parts[0], port, Username, Password);
            
            _onLoginSucceeded(parts[0], port, Username, Password);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    partial void OnIsConnectingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotConnecting));
        OnPropertyChanged(nameof(ConnectButtonText));

        if (value)
        {
            _connectionDots = 0;
            
            UpdateConnectionStatusText();
            
            _connectionStatusTimer = new Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(350)
            };
            
            _connectionStatusTimer.Tick += OnConnectionStatusTimerTick;
            _connectionStatusTimer.Start();
        }
        else if (_connectionStatusTimer is not null)
        {
            _connectionStatusTimer.Stop();
            _connectionStatusTimer.Tick -= OnConnectionStatusTimerTick;
            _connectionStatusTimer = null;
        }
    }

    private void OnConnectionStatusTimerTick(object? sender, EventArgs e)
    {
        _connectionDots = _connectionDots % 3 + 1;
        
        UpdateConnectionStatusText();
    }

    private void UpdateConnectionStatusText()
    {
        ConnectionStatusDots = new string('.', _connectionDots);
        
        OnPropertyChanged(nameof(ConnectionStatusDots));
    }

    partial void OnIsPasswordVisibleChanged(bool value)
        => OnPropertyChanged(nameof(PasswordChar));

    partial void OnErrorMessageChanged(string? value) 
        => OnPropertyChanged(nameof(HasError));

    partial void OnSelectedProfileChanged(ConnectionProfile? value)
    {
        if (value is null)
            return;

        _isApplyingProfile = true;
        Host = value.Host;
        Username = value.Username;
        Password = value.Password;
        _isApplyingProfile = false;
        IsCurrentProfileSaved = true;
        ErrorMessage = null;
    }

    partial void OnHostChanged(string value) 
        => MarkProfileAsUnsaved();

    partial void OnUsernameChanged(string value) 
        => MarkProfileAsUnsaved();

    partial void OnPasswordChanged(string value) 
        => MarkProfileAsUnsaved();

    private void MarkProfileAsUnsaved()
    {
        if (_isApplyingProfile)
            return;
        
        IsCurrentProfileSaved = false;
        SelectedProfile = null;
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            IReadOnlyList<ConnectionProfile> profiles = await _profileService.LoadProfilesAsync();

            foreach (ConnectionProfile profile in profiles)
                SavedProfiles.Add(profile);

            OnPropertyChanged(nameof(HasProfiles));
            OnPropertyChanged(nameof(ShowEmptyProfilesMessage));
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not load saved profiles: {exception.Message}";
        }
    }
}