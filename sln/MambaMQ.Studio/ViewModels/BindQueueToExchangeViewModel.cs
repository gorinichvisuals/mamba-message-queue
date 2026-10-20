namespace MambaMQ.Studio.ViewModels;

public partial class BindQueueToExchangeViewModel : ViewModelBase
{
    public IReadOnlyList<string> QueueNames { get; }

    [ObservableProperty]
    private string? _selectedQueueName;

    [ObservableProperty]
    private string _routingKey = string.Empty;

    public BindQueueToExchangeViewModel(IEnumerable<string> queueNames)
    {
        QueueNames = queueNames.ToArray();
        _selectedQueueName = QueueNames.FirstOrDefault();
    }
}