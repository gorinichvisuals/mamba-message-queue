namespace MambaMQ.Core.Services.Abstractions;

public interface IExchangeManager
{
    IReadOnlyList<ExchangeModel> GetExchanges();
    Task<CommandResponse> CreateExchange(ExchangeCreateDto exchangeCreateDto);
    Task<CommandResponse> UpdateExchange(ExchangeUpdateDto exchangeUpdateDto, CancellationToken cancellationToken = default);
    Task<CommandResponse> DeleteExchange(Guid exchangeId, IClientConnection connection, CancellationToken cancellationToken = default);
    Task<CommandResponse> Bind(ExchangeBindingDto dto, IClientConnection connection);
    Task<CommandResponse> Unbind(ExchangeBindingDto dto, IClientConnection connection);
    CommandResponse<IReadOnlyList<string>> ResolveQueues(string exchangeName, string routingKey, IClientConnection connection);
    Task RestoreExchanges(CancellationToken cancellationToken = default);
}