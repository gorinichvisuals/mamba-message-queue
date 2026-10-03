namespace MambaMQ.Core.Services.Abstractions;

public interface IExchangeManager
{
    Task CreateExchange(string name, bool isDurable, ExchangeType type);
    Task Bind(string exchangeName, string queueName, string routingKey);
    Task Unbind(string exchangeName, string queueName, string routingKey);
    IReadOnlyList<string> ResolveQueues(string exchangeName, string routingKey);
    Task RestoreExchanges(CancellationToken cancellationToken = default);
}