namespace MambaMQ.Protocol.Responses.Models.Queues;

public sealed class QueueModel : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsDurable { get; init; }
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; init; }
    public Dictionary<string, QueuePermission> Permissions { get; init; } = [];
    public bool MessageRetentionEnabled { get; init; }
    public TimeSpan MessageRetentionPeriod { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    private int _availableMessageCount;
    private int _inFlightMessageCount;
    public int AvailableMessageCount { get => _availableMessageCount; init => _availableMessageCount = value; }
    public int InFlightMessageCount { get => _inFlightMessageCount; init => _inFlightMessageCount = value; }

    public void UpdateStatistics(int available, int inFlight)
    {
        if (_availableMessageCount != available)
        {
            _availableMessageCount = available;
            PropertyChanged?.Invoke(this, new(nameof(AvailableMessageCount)));
        }
        if (_inFlightMessageCount != inFlight)
        {
            _inFlightMessageCount = inFlight;
            PropertyChanged?.Invoke(this, new(nameof(InFlightMessageCount)));
        }
    }
    
    public string PermissionsDisplay =>
        Permissions.Count == 0
            ? "—"
            : string.Join(
                ", ",
                Permissions.Select(x => $"{x.Key}: {x.Value}"));
}