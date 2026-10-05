namespace MambaMQ.Studio.Models;

public sealed class ServicePermissionModel
{
    public required string ServiceName { get; init; }

    public QueuePermission Permissions { get; init; }
}