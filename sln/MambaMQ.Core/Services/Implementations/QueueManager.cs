namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class QueueManager(
    bool authorizationEnabled,
    IServerStorageService serverStorageService, 
    IMambaLogger mambaLogger) : IQueueManager
{
    private readonly Dictionary<Guid, MambaQueue> _queues = [];
    private readonly Dictionary<string, Guid> _queueNames = [];
    private readonly Dictionary<Guid, List<Subscriber>> _subscribers = [];  
    private readonly Dictionary<Guid, ILoadBalancingStrategy> _loadBalancingStrategies = [];
    private readonly Dictionary<Guid, SemaphoreSlim> _batchLocks = [];

    private readonly ILogger _serverLogger = mambaLogger.Server;
    
    private bool HasQueuePermission(MambaQueue queue, IClientConnection connection, QueuePermission permission)
    {
        if (connection.ClientType is ClientType.Management)
            return true;
        
        if (!authorizationEnabled)
            return true;

        return connection?.ServiceName is not null && queue.HasPermission(connection.ServiceName, permission);
    }
    
    private MambaQueue? GetQueueByName(string queueName)
        => !_queueNames.TryGetValue(queueName, out Guid queueId) 
            ? null 
            : _queues[queueId];
}