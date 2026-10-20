namespace MambaMQ.Studio.ViewModels;

public partial class CreateExchangeViewModel : ViewModelBase
{
    public IReadOnlyList<ExchangeType> ExchangeTypes { get; } = Enum.GetValues<ExchangeType>();

    [ObservableProperty]
    private string _exchangeName = string.Empty;

    [ObservableProperty]
    private bool _isDurable = true;

    [ObservableProperty]
    private ExchangeType _exchangeType = ExchangeType.Direct;
}
