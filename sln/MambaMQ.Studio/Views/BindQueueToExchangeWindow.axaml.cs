using MambaMQ.Studio.Controls;

namespace MambaMQ.Studio.Views;

public partial class BindQueueToExchangeWindow : Window
{
    public BindQueueToExchangeWindow(IEnumerable<string> queueNames)
    {
        InitializeComponent();
        WindowChrome.Apply(this);
        
        DataContext = new BindQueueToExchangeViewModel(queueNames);
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => Close(null);

    private void BindButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not BindQueueToExchangeViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.SelectedQueueName))
            return;
        
        Close(new ExchangeBindingModel
        {
            QueueName = viewModel.SelectedQueueName,
            RoutingKey = viewModel.RoutingKey
        });
    }
}