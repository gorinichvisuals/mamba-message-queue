namespace MambaMQ.Studio.ViewModels;

public partial class LoginViewModel(
    IMambaStudioClient mambaClient,
    Action<string, int, string> onLoginSucceeded) : ViewModelBase
{
    [ObservableProperty]
    public partial string Host { get; set; } = string.Empty;
    
    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty] 
    private partial bool IsPasswordVisible { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public char? PasswordChar => IsPasswordVisible 
        ? null 
        : '*';

    public string PasswordVisibilityText => IsPasswordVisible 
        ? "Hide" 
        : "Show";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    partial void OnIsPasswordVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordChar));
        OnPropertyChanged(nameof(PasswordVisibilityText));
    }

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        string[] parts = Host.Split(':', 2);

        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || !int.TryParse(parts[1], out int port) || port is < 1 or > 65535)
        {
            ErrorMessage = "Invalid host or port.";
            
            return;
        }

        try
        {
            await mambaClient.ConnectAsync(parts[0], port, Username, Password);
            
            onLoginSucceeded(parts[0], port, Username);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }
}