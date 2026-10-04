namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class ExchangeManager
{
    private bool HasExchangePermission(MambaExchange exchange, IClientConnection connection, ExchangePermission permission)
    {
        if (!authorizationEnabled)
            return true;

        return connection?.ServiceName is not null && exchange.HasPermission(connection.ServiceName, permission);
    }
    
    private async Task<CommandResponse> PersistExchange(MambaExchange exchange, CancellationToken cancellationToken = default)
    {
        StoredMambaExchange storedExchange = new(
            exchange.Id,
            exchange.Name,
            exchange.IsDurable,
            (byte)exchange.Type,
            exchange.Permissions.ToDictionary(
                x => x.Key,
                x => (byte)x.Value),
            exchange.Bindings
                .Select(binding => new StoredExchangeBinding(
                    binding.QueueName,
                    binding.RoutingKey))
                .ToList());

        try
        {
            await serverStorageService.SaveExchange(storedExchange, cancellationToken);

            return CommandResponse.Success();
        }
        catch (Exception exception)
        {
            mambaLogger.Exchange(exchange.Name).LogError(exception, "Failed to persist exchange.");

            return CommandResponse.Fail(ErrorCode.PersistenceError, "Failed to persist exchange.");
        }
    }
    
    private static MambaExchange RestoreExchange(StoredMambaExchange storedExchange)
    {
        MambaExchange exchange = new(
            storedExchange.Id,
            storedExchange.Name,
            storedExchange.IsDurable,
            (ExchangeType)storedExchange.Type,
            storedExchange.Permissions.ToDictionary(
                x => x.Key,
                x => (ExchangePermission)x.Value));

        foreach (StoredExchangeBinding binding in storedExchange.Bindings)
            exchange.Bind(binding.QueueName, binding.RoutingKey);

        return exchange;
    }
    
    private void RegisterExchange(MambaExchange exchange)
    {
        _exchangesById.Add(exchange.Id, exchange);
        _exchangesByName.Add(exchange.Name, exchange);
    }
}