namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class ExchangeManager(
    bool authorizationEnabled,
    IServerStorageService serverStorageService,
    IMambaLogger mambaLogger) : IExchangeManager
{
    private readonly Dictionary<Guid, MambaExchange> _exchangesById = [];
    private readonly Dictionary<string, MambaExchange> _exchangesByName = [];

    private readonly ILogger _serverLogger = mambaLogger.Server;

    public IReadOnlyList<ExchangeModel> GetExchanges()
    {
        return _exchangesByName.Values
            .OrderBy(exchange => exchange.Name)
            .Select(MapToExchangeModel)
            .ToList();
    }

    public async Task<CommandResponse<ExchangeModel>> CreateExchange(ExchangeCreateDto dto)
    {
        if (_exchangesByName.ContainsKey(dto.Name))
        {
            _serverLogger.LogInformation("Exchange '{Name}' already exists.", dto.Name);

            return CommandResponse<ExchangeModel>.Success(MapToExchangeModel(_exchangesByName[dto.Name]));
        }

        MambaExchange exchange = new(
            Guid.CreateVersion7(),
            dto.Name,
            dto.IsDurable,
            DateTimeOffset.UtcNow,
            dto.Type,
            dto.Permissions);

        if (dto.IsDurable)
            await PersistExchange(exchange);

        RegisterExchange(exchange);

        mambaLogger.Exchange(exchange.Name).LogInformation("Exchange was created.");

        return CommandResponse<ExchangeModel>.Success(MapToExchangeModel(exchange));
    }

    public async Task<CommandResponse<ExchangeModel>> UpdateExchange(ExchangeUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (!_exchangesById.TryGetValue(dto.ExchangeId, out MambaExchange? current))
            return CommandResponse<ExchangeModel>.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{dto.ExchangeId}' does not exist.");

        if (_exchangesByName.TryGetValue(dto.Name, out MambaExchange? sameName) && sameName.Id != dto.ExchangeId)
            return CommandResponse<ExchangeModel>.Fail(ErrorCode.InvalidArgument, $"Exchange '{dto.Name}' already exists.");

        MambaExchange updated = new(
            current.Id,
            dto.Name,
            dto.IsDurable,
            current.CreatedAt,
            dto.Type,
            new Dictionary<string, ExchangePermission>(dto.Permissions));

        foreach (ExchangeBinding binding in current.Bindings)
            updated.Bind(binding.QueueName, binding.RoutingKey);

        if (current.IsDurable && !updated.IsDurable)
        {
            try
            {
                await serverStorageService.DeleteExchange(current.Id, cancellationToken);
            }
            catch (Exception exception)
            {
                mambaLogger.Exchange(current.Name).LogError(exception, "Failed to delete persisted exchange while updating it.");

                return CommandResponse<ExchangeModel>.Fail(ErrorCode.PersistenceError, "Failed to update exchange persistence.");
            }
        }

        if (updated.IsDurable)
        {
            CommandResponse persistResponse = await PersistExchange(updated, cancellationToken);

            if (!persistResponse.IsSucceed)
                return CommandResponse<ExchangeModel>.Fail(persistResponse.ErrorCode, persistResponse.ErrorMessage ?? "Failed to update exchange persistence.");
        }

        _exchangesByName.Remove(current.Name);
        _exchangesById[updated.Id] = updated;
        _exchangesByName[updated.Name] = updated;

        mambaLogger.Exchange(updated.Name).LogInformation("Exchange was updated.");

        return CommandResponse<ExchangeModel>.Success(MapToExchangeModel(updated));
    }

    private static ExchangeModel MapToExchangeModel(MambaExchange exchange)
        => new()
        {
            Id = exchange.Id,
            Name = exchange.Name,
            IsDurable = exchange.IsDurable,
            CreatedAt = exchange.CreatedAt,
            Type = exchange.Type,
            Permissions = new Dictionary<string, ExchangePermission>(exchange.Permissions),
            Bindings = exchange.Bindings
                .Select(binding => new ExchangeBindingModel
                {
                    QueueName = binding.QueueName,
                    RoutingKey = binding.RoutingKey
                })
                .ToList()
        };

    public async Task<CommandResponse> DeleteExchange(Guid exchangeId, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        if (!_exchangesById.TryGetValue(exchangeId, out MambaExchange? exchange))
            return CommandResponse.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{exchangeId}' does not exist.");

        if (!HasExchangePermission(exchange, connection, ExchangePermission.Delete))
        {
            mambaLogger.Exchange(exchange.Name).LogWarning("Service '{ServiceName}' has no Delete permission for this exchange.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Delete permission for exchange '{exchange.Name}'.");
        }

        if (exchange.IsDurable)
        {
            try
            {
                await serverStorageService.DeleteExchange(exchange.Id, cancellationToken);
            }
            catch (Exception exception)
            {
                mambaLogger.Exchange(exchange.Name).LogError(exception, "Failed to delete persisted exchange.");

                return CommandResponse.Fail(ErrorCode.PersistenceError, "Failed to delete exchange.");
            }
        }

        _exchangesById.Remove(exchange.Id);
        _exchangesByName.Remove(exchange.Name);

        mambaLogger.Exchange(exchange.Name).LogInformation("Exchange was deleted.");

        return CommandResponse.Success();
    }

    public async Task<CommandResponse> Bind(ExchangeBindingDto dto, IClientConnection connection)
    {
        if (!_exchangesByName.TryGetValue(dto.ExchangeName, out MambaExchange? exchange))
            return CommandResponse.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{dto.ExchangeName}' does not exist.");

        if (!HasExchangePermission(exchange, connection, ExchangePermission.Write))
        {
            mambaLogger.Exchange(exchange.Name).LogWarning("Service '{ServiceName}' has no Write permission for this exchange.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Write permission for exchange '{exchange.Name}'.");
        }

        bool alreadyBound = exchange.Bindings.Any(binding =>
            binding.QueueName == dto.QueueName && binding.RoutingKey == dto.RoutingKey);
        exchange.Bind(dto.QueueName, dto.RoutingKey);

        if (exchange.IsDurable)
        {
            CommandResponse persistResponse = await PersistExchange(exchange);
            if (!persistResponse.IsSucceed)
            {
                if (!alreadyBound)
                    exchange.Unbind(dto.QueueName, dto.RoutingKey);
                return persistResponse;
            }
        }

        mambaLogger.Exchange(exchange.Name).LogInformation("Queue {QueueId} was bound with routing key '{RoutingKey}'.", dto.QueueName, dto.RoutingKey);

        return CommandResponse.Success();
    }

    public async Task<CommandResponse> Unbind(ExchangeBindingDto dto, IClientConnection connection)
    {
        if (!_exchangesByName.TryGetValue(dto.ExchangeName, out MambaExchange? exchange))
            return CommandResponse.Fail(ErrorCode.ExchangeNotFound, $"Exchange '{dto.ExchangeName}' does not exist.");

        if (!HasExchangePermission(exchange, connection, ExchangePermission.Delete))
        {
            mambaLogger.Exchange(exchange.Name).LogWarning("Service '{ServiceName}' has no Delete permission for this exchange.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Delete permission for exchange '{exchange.Name}'.");
        }

        bool wasBound = exchange.Bindings.Any(binding =>
            binding.QueueName == dto.QueueName && binding.RoutingKey == dto.RoutingKey);
        exchange.Unbind(dto.QueueName, dto.RoutingKey);

        if (exchange.IsDurable)
        {
            CommandResponse persistResponse = await PersistExchange(exchange);
            if (!persistResponse.IsSucceed)
            {
                if (wasBound)
                    exchange.Bind(dto.QueueName, dto.RoutingKey);
                return persistResponse;
            }
        }

        mambaLogger.Exchange(exchange.Name).LogInformation("Queue {QueueName} was unbound with routing key '{RoutingKey}'.", dto.QueueName, dto.RoutingKey);

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

        _serverLogger.LogInformation("Successfully restored {ExchangeCount} exchanges.", storedExchanges.Count);

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