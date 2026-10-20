namespace MambaMQ.Studio.Views;

public partial class ExchangesView : UserControl
{
    public ExchangesView()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            if (DataContext is ExchangesViewModel viewModel)
                await viewModel.LoadCommand.ExecuteAsync(null);
        };
    }

    private async void CreateExchangeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner ||
            DataContext is not ExchangesViewModel viewModel)
            return;

        CreateExchangeWindow window = new();
        ExchangeOptions? options = await window.ShowDialog<ExchangeOptions?>(owner);
        if (options is not null)
            await viewModel.CreateExchangeCommand.ExecuteAsync(options);
    }

    private void ExchangeRow_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
            border.Background = new SolidColorBrush(Color.Parse("#383838"));
    }

    private void ExchangeRow_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
            border.Background = Brushes.Transparent;
    }
}
