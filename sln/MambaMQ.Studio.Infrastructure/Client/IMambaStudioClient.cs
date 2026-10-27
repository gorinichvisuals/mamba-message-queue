namespace MambaMQ.Studio.Infrastructure.Client;

public interface IMambaStudioClient
{
    Task ConnectAsync(string host, int port, string username, string password, CancellationToken cancellationToken = default);
    Task PingAsync(CancellationToken cancellationToken = default);
    ValueTask DisconnectAsync(CancellationToken cancellationToken = default);
    
    Task<GetExchangesResponse> GetExchangesAsync(CancellationToken cancellationToken = default);
    Task<CommandResponse<ExchangeModel>> CreateExchangeAsync(ExchangeOptions options, CancellationToken cancellationToken = default);
    Task<CommandResponse> BindQueueToExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default);
    Task<CommandResponse> UnbindQueueFromExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default);
    Task<CommandResponse> DeleteExchangeAsync(Guid exchangeId, CancellationToken cancellationToken = default);
    Task<CommandResponse<ExchangeModel>> UpdateExchangeAsync(Guid exchangeId, ExchangeOptions options, CancellationToken cancellationToken = default);
    
    Task<GetQueuesResponse> GetQueuesAsync(CancellationToken cancellationToken = default);
    Task<CommandResponse<QueueModel>> CreateQueueAsync(QueueOptions options, CancellationToken cancellationToken = default);
    Task<CommandResponse> DeleteQueueAsync(Guid queueId, CancellationToken cancellationToken = default);
    Task<CommandResponse<QueueModel>> UpdateQueueAsync(Guid queueId, QueueOptions options, CancellationToken cancellationToken = default);
    
    Task<CommandResponse> PublishMessageToExchangeAsync(string exchangeName, string routingKey, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default);
    Task<CommandResponse> PublishMessageToQueueAsync(string queueName, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default);
    Task<CommandResponse<GetMessagesResponse>> GetMessagesAsync(string queueName, int skip, int take, CancellationToken cancellationToken = default);
    Task<CommandResponse> DeleteMessagesAsync(string queueName, ICollection<Guid> messageIds, CancellationToken cancellationToken = default);
}