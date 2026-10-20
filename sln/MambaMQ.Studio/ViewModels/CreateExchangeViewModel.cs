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

    [ObservableProperty]
    private string _serviceName = string.Empty;

    [ObservableProperty]
    private bool _canRead;

    [ObservableProperty]
    private bool _canWrite;

    [ObservableProperty]
    private bool _canDelete;

    public ObservableCollection<ExchangePermissionModel> Permissions { get; } = [];

    [RelayCommand]
    private void AddPermission()
    {
        if (string.IsNullOrWhiteSpace(ServiceName))
            return;

        ExchangePermission permissions = ExchangePermission.None;
        
        if (CanRead)
            permissions |= ExchangePermission.Read;
        
        if (CanWrite)
            permissions |= ExchangePermission.Write;
        
        if (CanDelete)
            permissions |= ExchangePermission.Delete;
        
        if (permissions == ExchangePermission.None)
            return;

        string serviceName = ServiceName.Trim();
        
        ExchangePermissionModel? existing = Permissions.FirstOrDefault(permission => permission.ServiceName == serviceName);
        
        if (existing is not null)
            Permissions.Remove(existing);

        Permissions.Add(new ExchangePermissionModel
        {
            ServiceName = serviceName,
            Permissions = permissions
        });

        ServiceName = string.Empty;
        CanRead = false;
        CanWrite = false;
        CanDelete = false;
    }

    public void RemovePermission(ExchangePermissionModel permission)
    {
        Permissions.Remove(permission);
    }
}
