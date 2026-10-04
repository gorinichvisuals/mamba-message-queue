namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class ExchangeManager(
    bool authorizationEnabled,
    IServerStorageService serverStorageService, 
    IMambaLogger mambaLogger) : IExchangeManager
{
    private readonly Dictionary<Guid, MambaExchange> _exchangesById = [];
    private readonly Dictionary<string, MambaExchange> _exchangesByName = [];
    
    public async Task<CommandResponse> CreateExchange(string name, bool isDurable, ExchangeType type,
        Dictionary<string, ExchangePermission> permissions)
    {
        if (_exchangesByName.ContainsKey(name))
        {
            mambaLogger.Server.LogInformation("Exchange '{Name}' already exists.", name);
            
            return CommandResponse.Success();
        }

        MambaExchange exchange = new(
            Guid.CreateVersion7(), 
            name, 
            isDurable, 
            type,
            permissions);

        if (isDurable)
            await PersistExchange(exchange);
        
        RegisterExchange(exchange);
        
        mambaLogger.Exchange(exchange.Name).LogInformation("Exchange was created.");
        
        return CommandResponse.Success();
    }
    
    public async Task<CommandResponse> Bind(string exchangeName, string queueName, string routingKey, IClientConnection connection)
    {
        if (!_exchangesByName.TryGetValue(exchangeName, out MambaExchange? exchange))
            return CommandResponse.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{exchangeName}' does not exist.");
        
        if (!HasExchangePermission(exchange, connection, ExchangePermission.Write))
        {
            mambaLogger.Exchange(exchange.Name).LogWarning("Service '{ServiceName}' has no Write permission for this exchange.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Write permission for exchange '{exchange.Name}'.");
        }
        
        if (exchange.IsDurable)
            await PersistExchange(exchange);
        
        exchange.Bind(queueName, routingKey);

        mambaLogger.Exchange(exchange.Name).LogInformation("Queue {QueueId} was bound with routing key '{RoutingKey}'.", queueName, routingKey);

        return CommandResponse.Success();
    }

    public async Task<CommandResponse> Unbind(string exchangeName, string queueName, string routingKey, IClientConnection connection)
    {
        if (!_exchangesByName.TryGetValue(exchangeName, out MambaExchange? exchange))
            return CommandResponse.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{exchangeName}' does not exist.");
        
        if (!HasExchangePermission(exchange, connection, ExchangePermission.Delete))
        {
            mambaLogger.Exchange(exchange.Name).LogWarning("Service '{ServiceName}' has no Delete permission for this exchange.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Delete permission for exchange '{exchange.Name}'.");
        }
        
        if (exchange.IsDurable)
        {
            CommandResponse persistResponse = await PersistExchange(exchange);

            if (!persistResponse.IsSucceed)
                return persistResponse;
        }

        exchange.Unbind(queueName, routingKey);

        mambaLogger.Exchange(exchange.Name).LogInformation("Queue {QueueName} was unbound with routing key '{RoutingKey}'.", queueName, routingKey);

        return CommandResponse.Success();
    }

    public CommandResponse<IReadOnlyList<string>> ResolveQueues(string exchangeName, string routingKey, IClientConnection connection)
    {
        if (!_exchangesByName.TryGetValue(exchangeName, out MambaExchange? exchange))
            return CommandResponse<IReadOnlyList<string>>.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{exchangeName}' does not exist.");

        if (!HasExchangePermission(exchange, connection, ExchangePermission.Write))
        {
            mambaLogger.Exchange(exchange.Name).LogWarning("Service '{ServiceName}' has no Write permission for this exchange.", connection.ServiceName);

            return CommandResponse<IReadOnlyList<string>>.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Write permission for exchange '{exchange.Name}'.");
        }
        
        IReadOnlyList<string> queueNames = exchange.ResolveQueues(routingKey);

        return CommandResponse<IReadOnlyList<string>>.Success(queueNames);
    }
    
    public async Task RestoreExchanges(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<StoredMambaExchange> storedExchanges = await serverStorageService.RestoreExchanges(cancellationToken);

        mambaLogger.Server.LogInformation("Successfully restored {ExchangeCount} exchanges.", storedExchanges.Count);

        foreach (StoredMambaExchange storedExchange in storedExchanges)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MambaExchange exchange = RestoreExchange(storedExchange);

            RegisterExchange(exchange);

            mambaLogger.Exchange(exchange.Name).LogInformation("Restored {BindingsCount} bindings.", storedExchange.Bindings.Count);
            mambaLogger.Exchange(exchange.Name).LogInformation("Exchange was successfully restored.");
        }
    }
}