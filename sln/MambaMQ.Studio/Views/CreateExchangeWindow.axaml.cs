using MambaMQ.Studio.Controls;

namespace MambaMQ.Studio.Views;

public partial class CreateExchangeWindow : Window
{
    public CreateExchangeWindow()
    {
        InitializeComponent();
        WindowChrome.Apply(this);
        DataContext = new CreateExchangeViewModel();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void CreateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CreateExchangeViewModel viewModel ||
            string.IsNullOrWhiteSpace(viewModel.ExchangeName))
            return;

        ExchangeOptions options = new()
        {
            Name = viewModel.ExchangeName.Trim(),
            IsDurable = viewModel.IsDurable,
            Type = viewModel.ExchangeType,
            Permissions = viewModel.Permissions.ToDictionary(
                permission => permission.ServiceName,
                permission => permission.Permissions)
        };

        Close(options);
    }

    private void RemovePermissionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ExchangePermissionModel permission } &&
            DataContext is CreateExchangeViewModel viewModel)
            viewModel.RemovePermission(permission);
    }
}
