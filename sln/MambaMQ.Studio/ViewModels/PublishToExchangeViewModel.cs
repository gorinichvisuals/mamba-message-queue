namespace MambaMQ.Studio.ViewModels;

public partial class PublishToExchangeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _routingKey = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;
}