namespace MambaMQ.Core.Services.Abstractions;

public interface IExchangeManager
{
    Task<CommandResponse> CreateExchange(string name, bool isDurable, ExchangeType type, Dictionary<string, ExchangePermission> permissions);
    Task<CommandResponse> Bind(string exchangeName, string queueName, string routingKey, IClientConnection connection);
    Task<CommandResponse> Unbind(string exchangeName, string queueName, string routingKey, IClientConnection connection);
    CommandResponse<IReadOnlyList<string>> ResolveQueues(string exchangeName, string routingKey, IClientConnection connection);
    Task RestoreExchanges(CancellationToken cancellationToken = default);
}