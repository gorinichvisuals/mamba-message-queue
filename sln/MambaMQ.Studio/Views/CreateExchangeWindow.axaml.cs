namespace MambaMQ.Studio.Views;

public partial class CreateExchangeWindow : Window
{
    public CreateExchangeWindow()
    {
        InitializeComponent();
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
            Type = viewModel.ExchangeType
        };

        Close(options);
    }
}
