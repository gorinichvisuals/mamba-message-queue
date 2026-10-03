namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class ExchangeManager(
    IServerStorageService serverStorageService, 
    IMambaLogger mambaLogger) : IExchangeManager
{
    private readonly Dictionary<Guid, MambaExchange> _exchangesById = [];
    private readonly Dictionary<string, MambaExchange> _exchangesByName = [];
    
    public async Task CreateExchange(string name, bool isDurable, ExchangeType type)
    {
        if (_exchangesByName.ContainsKey(name))
        {
            mambaLogger.Server.LogInformation("Exchange '{Name}' already exists.", name);
            
            return;
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
    }
    
    public async Task Bind(string exchangeName, string queueName, string routingKey)
    {
        MambaExchange exchange = GetRequiredExchange(exchangeName);
        
        if (exchange.IsDurable)
            await PersistExchange(exchange);
        
        exchange.Bind(queueName, routingKey);
        
        mambaLogger.Exchange(exchange.Name).LogInformation("Queue {QueueId} was bound with routing key '{RoutingKey}'.", queueName, routingKey);
    }

    public async Task Unbind(string exchangeName, string queueName, string routingKey)
    {
        MambaExchange exchange = GetRequiredExchange(exchangeName);
        
        if (exchange.IsDurable)
            await PersistExchange(exchange);
        
        exchange.Unbind(queueName, routingKey);
        
        mambaLogger.Exchange(exchange.Name).LogInformation("Queue {queueName} was unbound with routing key '{RoutingKey}'.", queueName, routingKey);
    }

    public IReadOnlyList<string> ResolveQueues(string exchangeName, string routingKey)
    {
        MambaExchange exchange = GetRequiredExchange(exchangeName);

        return exchange.ResolveQueues(routingKey);
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