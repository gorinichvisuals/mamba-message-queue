namespace MambaMQ.Studio.Views;

public partial class ProfileEditorWindow : Window
{
    private readonly ConnectionProfile _profile;

    public ProfileEditorWindow(ConnectionProfile profile, bool isEditing)
    {
        InitializeComponent();
        
        WindowChrome.Apply(this);
        
        _profile = profile;
        
        HeadingText.Text = isEditing 
            ? "Update connection profile" 
            : "Name this connection profile";
        
        Title = isEditing 
            ? "Update profile" 
            : "Save profile";
        
        NameInput.Text = profile.Name;
        HostInput.Text = profile.Host;
        UsernameInput.Text = profile.Username;
        PasswordInput.Text = profile.Password;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        string name = NameInput.Text?.Trim() ?? string.Empty;
        string host = HostInput.Text?.Trim() ?? string.Empty;
        string username = UsernameInput.Text?.Trim() ?? string.Empty;
        string password = PasswordInput.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            ValidationText.Text = "Enter a profile name, host, username, and password.";
            ValidationText.IsVisible = true;
            return;
        }

        _profile.Name = name;
        _profile.Host = host;
        _profile.Username = username;
        _profile.Password = password;
        Close(_profile);
    }
}