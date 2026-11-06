namespace MambaMQ.Studio.Views;

public partial class ConnectView : UserControl
{
    public ConnectView()
    {
        InitializeComponent();
    }

    private void OnWindowDragPointerPressed(object? sender, PointerPressedEventArgs e)
        => WindowDragHelper.TryBeginMoveDrag(this, e);

    private void OnProfilesPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed || e.Source is not Control source)
            return;

        Control? element = source;

        while (element is not null && element is not ListBoxItem)
            element = element.GetVisualParent() as Control;

        if (element?.DataContext is ConnectionProfile profile)
            this.FindControl<ListBox>("ProfilesList")!.SelectedItem = profile;
    }

    private void OnDeleteProfileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectViewModel viewModel && viewModel.DeleteProfileCommand.CanExecute(null))
            viewModel.DeleteProfileCommand.Execute(null);
    }

    private async void OnUpdateProfileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConnectViewModel viewModel || viewModel.GetProfileForEditing() is not { } profile || TopLevel.GetTopLevel(this) is not Window owner)
            return;

        ProfileEditorWindow editor = new(profile, isEditing: true);
        ConnectionProfile? updatedProfile = await editor.ShowDialog<ConnectionProfile?>(owner);

        if (updatedProfile is not null)
            await viewModel.SaveProfileAsync(updatedProfile);
    }

    private async void OnSaveProfileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConnectViewModel viewModel || TopLevel.GetTopLevel(this) is not Window owner)
            return;

        ConnectionProfile profile = viewModel.CreateProfileFromCurrentConnection(string.Empty);
        ProfileEditorWindow editor = new(profile, isEditing: false);
        ConnectionProfile? savedProfile = await editor.ShowDialog<ConnectionProfile?>(owner);

        if (savedProfile is not null)
            await viewModel.SaveProfileAsync(savedProfile);
    }

    private async void OnConnectClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConnectViewModel viewModel)
            return;

        if (!viewModel.IsCurrentProfileSaved && TopLevel.GetTopLevel(this) is Window owner)
        {
            SaveProfilePromptWindow prompt = new();
            bool shouldSave = await prompt.ShowDialog<bool>(owner);

            if (shouldSave)
            {
                ConnectionProfile profile = viewModel.CreateProfileFromCurrentConnection(string.Empty);
                ProfileEditorWindow editor = new(profile, isEditing: false);
                ConnectionProfile? savedProfile = await editor.ShowDialog<ConnectionProfile?>(owner);

                if (savedProfile is null || !await viewModel.SaveProfileAsync(savedProfile))
                    return;
            }
        }

        await viewModel.ConnectAsync();
    }
}