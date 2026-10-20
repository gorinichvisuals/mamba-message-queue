namespace MambaMQ.Studio.Models;

public sealed class ExchangePermissionModel
{
    public required string ServiceName { get; init; }
    public ExchangePermission Permissions { get; init; }
}
