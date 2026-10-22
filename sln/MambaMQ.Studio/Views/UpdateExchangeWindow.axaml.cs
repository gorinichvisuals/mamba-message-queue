using MambaMQ.Studio.Controls;

namespace MambaMQ.Studio.Views;

public partial class UpdateExchangeWindow : Window
{
    public UpdateExchangeWindow(ExchangeModel exchange)
    {
        InitializeComponent();
        WindowChrome.Apply(this);
        
        DataContext = new UpdateExchangeViewModel(exchange);
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => Close(null);

    private void UpdateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UpdateExchangeViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.ExchangeName))
            return;
        
        Close(viewModel.ToOptions());
    }

    private void RemovePermissionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ExchangePermissionModel permission } &&
            DataContext is UpdateExchangeViewModel viewModel)
            viewModel.RemovePermission(permission);
    }
}
