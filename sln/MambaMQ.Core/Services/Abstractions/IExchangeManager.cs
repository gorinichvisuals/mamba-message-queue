namespace MambaMQ.Core.Services.Abstractions;

public interface IExchangeManager
{
    Task<CommandResponse> CreateExchange(string name, bool isDurable, ExchangeType type);
    Task<CommandResponse> Bind(string exchangeName, string queueName, string routingKey);
    Task<CommandResponse> Unbind(string exchangeName, string queueName, string routingKey);
    CommandResponse<IReadOnlyList<string>> ResolveQueues(string exchangeName, string routingKey);
    Task RestoreExchanges(CancellationToken cancellationToken = default);
}