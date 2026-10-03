namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class ExchangeManager(
    IServerStorageService serverStorageService, 
    IMambaLogger mambaLogger) : IExchangeManager
{
    private readonly Dictionary<Guid, MambaExchange> _exchangesById = [];
    private readonly Dictionary<string, MambaExchange> _exchangesByName = [];
    
    public async Task<CommandResponse> CreateExchange(string name, bool isDurable, ExchangeType type)
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
            type);

        if (isDurable)
            await PersistExchange(exchange);
        
        _exchangesById.Add(exchange.Id, exchange);
        _exchangesByName.Add(exchange.Name, exchange);
        
        mambaLogger.Exchange(exchange.Name).LogInformation("Exchange was created.");
        
        return CommandResponse.Success();
    }
    
    public async Task<CommandResponse> Bind(
        string exchangeName,
        string queueName,
        string routingKey)
    {
        if (!_exchangesByName.TryGetValue(exchangeName, out MambaExchange? exchange))
            return CommandResponse.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{exchangeName}' does not exist.");
        
        if (exchange.IsDurable)
            await PersistExchange(exchange);
        
        exchange.Bind(queueName, routingKey);

        mambaLogger.Exchange(exchange.Name).LogInformation("Queue {QueueId} was bound with routing key '{RoutingKey}'.", queueName, routingKey);

        return CommandResponse.Success();
    }

    public async Task<CommandResponse> Unbind(string exchangeName, string queueName, string routingKey)
    {
        if (!_exchangesByName.TryGetValue(exchangeName, out MambaExchange? exchange))
            return CommandResponse.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{exchangeName}' does not exist.");
        
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

    public CommandResponse<IReadOnlyList<string>> ResolveQueues(string exchangeName, string routingKey)
    {
        if (!_exchangesByName.TryGetValue(exchangeName, out MambaExchange? exchange))
            return CommandResponse<IReadOnlyList<string>>.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{exchangeName}' does not exist.");

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

            MambaExchange exchange = new(
                storedExchange.Id,
                storedExchange.Name,
                storedExchange.IsDurable,
                (ExchangeType)storedExchange.Type);

            foreach (StoredExchangeBinding binding in storedExchange.Bindings)
                exchange.Bind(binding.QueueName, binding.RoutingKey);

            _exchangesById.Add(exchange.Id, exchange);
            _exchangesByName.Add(exchange.Name, exchange);

            mambaLogger.Exchange(exchange.Name).LogInformation("Restored {BindingsCount} bindings.", storedExchange.Bindings.Count);

            mambaLogger.Exchange(exchange.Name).LogInformation("Exchange was successfully restored.");
        }
    }
}