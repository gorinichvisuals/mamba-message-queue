namespace MambaMQ.Studio.ViewModels;

public partial class UpdateQueueViewModel : ViewModelBase
{
    public IReadOnlyList<LoadBalancingAlgorithm> LoadBalancingAlgorithms { get; } =
        Enum.GetValues<LoadBalancingAlgorithm>();

    [ObservableProperty]
    private string _queueName = string.Empty;

    [ObservableProperty]
    private bool _isDurable;

    [ObservableProperty]
    private LoadBalancingAlgorithm _loadBalancingAlgorithm;

    [ObservableProperty]
    private bool _messageRetentionEnabled;

    [ObservableProperty]
    private double _retentionPeriod;

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

    public UpdateQueueViewModel(QueueModel queue)
    {
        QueueName = queue.Name;
        IsDurable = queue.IsDurable;
        LoadBalancingAlgorithm = queue.LoadBalancingAlgorithm;
        MessageRetentionEnabled = queue.MessageRetentionEnabled;

        RetentionPeriod = queue.MessageRetentionPeriod.TotalMinutes;

        foreach (KeyValuePair<string, QueuePermission> permission in queue.Permissions)
        {
            Permissions.Add(new ServicePermissionModel
            {
                ServiceName = permission.Key,
                Permissions = permission.Value
            });
        }
    }

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

    public void RemovePermission(ServicePermissionModel permission)
    {
        Permissions.Remove(permission);
    }

    public QueueOptions ToOptions()
    {
        return new QueueOptions
        {
            QueueName = QueueName,
            IsDurable = IsDurable,
            LoadBalancingAlgorithm = LoadBalancingAlgorithm,
            Permissions = Permissions.ToDictionary(
                x => x.ServiceName,
                x => x.Permissions),
            MessageRetention = new MessageRetentionOptions
            {
                Enabled = MessageRetentionEnabled,
                RetentionPeriod = TimeSpan.FromMinutes(RetentionPeriod)
            }
        };
    }
}