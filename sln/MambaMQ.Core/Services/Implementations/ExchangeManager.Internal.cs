namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class ExchangeManager
{
    private static CommandResponse ValidateExchangeOptions(
        string name,
        ExchangeType type,
        Dictionary<string, ExchangePermission>? permissions)
    {
        if (string.IsNullOrWhiteSpace(name))
            return CommandResponse.Fail(ErrorCode.InvalidArgument, "Exchange name cannot be empty or whitespace.");

        if (!Enum.IsDefined(type))
            return CommandResponse.Fail(ErrorCode.InvalidArgument, "Unknown exchange type.");

        if (permissions is null)
            return CommandResponse.Fail(ErrorCode.InvalidArgument, "Exchange permissions cannot be null.");

        foreach ((string serviceName, ExchangePermission permission) in permissions)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return CommandResponse.Fail(ErrorCode.InvalidArgument, "Permission service names cannot be empty or whitespace.");

            if ((permission & ~(ExchangePermission.Read | ExchangePermission.Write | ExchangePermission.Delete)) != 0)
                return CommandResponse.Fail(ErrorCode.InvalidArgument, $"Permissions for service '{serviceName}' contain unknown flags.");
        }

        return CommandResponse.Success();
    }

    private static CommandResponse ValidateBinding(ExchangeBindingDto? dto)
    {
        if (dto is null)
            return CommandResponse.Fail(ErrorCode.InvalidArgument, "Exchange binding cannot be null.");

        if (string.IsNullOrWhiteSpace(dto.ExchangeName))
            return CommandResponse.Fail(ErrorCode.InvalidArgument, "Exchange name cannot be empty or whitespace.");

        return string.IsNullOrWhiteSpace(dto.QueueName) 
            ? CommandResponse.Fail(ErrorCode.InvalidArgument, "Queue name cannot be empty or whitespace.") 
            : CommandResponse.Success();
    }

    private bool HasExchangePermission(MambaExchange exchange, IClientConnection connection, ExchangePermission permission)
    {
        if (connection.ClientType is ClientType.Management)
            return true;
        
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
            exchange.CreatedAt,
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
            storedExchange.CreatedAt,
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