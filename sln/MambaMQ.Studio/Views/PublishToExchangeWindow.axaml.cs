using MambaMQ.Studio.Controls;

namespace MambaMQ.Studio.Views;

public partial class PublishToExchangeWindow : Window
{
    public PublishToExchangeWindow()
    {
        InitializeComponent();
        WindowChrome.Apply(this);
        
        DataContext = new PublishToExchangeViewModel();
        
        Opened += (_, _) => MessageTextBox.Focus();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => Close(null);

    private void SendButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PublishToExchangeViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.Message))
            return;
        
        Close(new PublishToExchangeOptions(viewModel.RoutingKey, viewModel.Message));
    }
}