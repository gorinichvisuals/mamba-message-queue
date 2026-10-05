namespace MambaMQ.Studio.ViewModels;

public partial class CreateQueueViewModel : ViewModelBase
{
    public IReadOnlyList<LoadBalancingAlgorithm> LoadBalancingAlgorithms { get; } =
        Enum.GetValues<LoadBalancingAlgorithm>();

    public IReadOnlyList<QueuePermission> QueuePermissions { get; } =
        Enum.GetValues<QueuePermission>()
            .Where(x => x != QueuePermission.None)
            .ToArray();

    [ObservableProperty]
    private string _queueName = string.Empty;

    [ObservableProperty]
    private bool _isDurable = true;

    [ObservableProperty]
    private LoadBalancingAlgorithm _loadBalancingAlgorithm = LoadBalancingAlgorithm.RoundRobin;

    [ObservableProperty]
    private bool _messageRetentionEnabled;

    [ObservableProperty]
    private double _retentionPeriod = 10;

    [ObservableProperty]
    private string _serviceName = string.Empty;

    [ObservableProperty]
    private bool _canRead;

    [ObservableProperty]
    private bool _canWrite;

    [ObservableProperty]
    private bool _canDelete;

    public ObservableCollection<ServicePermissionModel> Permissions { get; } = [];

    public bool IsRetentionPeriodEnabled => MessageRetentionEnabled;

    partial void OnMessageRetentionEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(IsRetentionPeriodEnabled));
    }

    [RelayCommand]
    private void AddPermission()
    {
        if (string.IsNullOrWhiteSpace(ServiceName))
            return;

        QueuePermission permissions = QueuePermission.None;

        if (CanRead)
            permissions |= QueuePermission.Read;

        if (CanWrite)
            permissions |= QueuePermission.Write;

        if (CanDelete)
            permissions |= QueuePermission.Delete;

        if (permissions == QueuePermission.None)
            return;

        Permissions.Add(new ServicePermissionModel
        {
            ServiceName = ServiceName.Trim(),
            Permissions = permissions
        });

        ServiceName = string.Empty;
        CanRead = false;
        CanWrite = false;
        CanDelete = false;
    }

    [RelayCommand]
    private void RemovePermission(ServicePermissionModel permission)
    {
        Permissions.Remove(permission);
    }
}