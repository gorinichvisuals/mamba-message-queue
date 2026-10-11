namespace MambaMQ.Core.Services.Abstractions;

public interface IExchangeManager
{
    Task<CommandResponse> CreateExchange(ExchangeCreateDto exchangeCreateDto);
    Task<CommandResponse> Bind(ExchangeBindingDto dto, IClientConnection connection);
    Task<CommandResponse> Unbind(ExchangeBindingDto dto, IClientConnection connection);
    CommandResponse<IReadOnlyList<string>> ResolveQueues(string exchangeName, string routingKey, IClientConnection connection);
    Task RestoreExchanges(CancellationToken cancellationToken = default);
}