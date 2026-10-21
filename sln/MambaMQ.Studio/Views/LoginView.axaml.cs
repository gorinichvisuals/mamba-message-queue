namespace MambaMQ.Studio.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void OnProfilesPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed || e.Source is not Control source)
            return;

        Control? element = source;

        while (element is not null && element is not ListBoxItem)
            element = element.GetVisualParent() as Control;

        if (element?.DataContext is ConnectionProfile profile)
            ProfilesList.SelectedItem = profile;
    }

    private void OnDeleteProfileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel && viewModel.DeleteProfileCommand.CanExecute(null))
            viewModel.DeleteProfileCommand.Execute(null);
    }

    private async void OnConnectClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel viewModel)
            return;

        if (!viewModel.IsCurrentProfileSaved && TopLevel.GetTopLevel(this) is Window owner)
        {
            SaveProfilePromptWindow prompt = new();
            bool shouldSave = await prompt.ShowDialog<bool>(owner);

            if (shouldSave && !await viewModel.SaveProfileForConnectionAsync())
                return;
        }

        await viewModel.LoginAsync();
    }
}