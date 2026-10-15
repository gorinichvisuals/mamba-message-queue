namespace MambaMQ.Core.Services.Abstractions;

public interface IQueueManager
{
    public IReadOnlyList<QueueModel> GetQueues();
    Task<CommandResponse<QueueModel>> CreateQueue(QueueCreateDto dto);
    Task<CommandResponse<QueueModel>> UpdateQueue(QueueUpdateDto dto, CancellationToken cancellationToken = default);
    Task<CommandResponse> DeleteQueue(Guid queueId, IClientConnection connection, CancellationToken cancellationToken = default);
    
    Task<CommandResponse> SubscribeQueue(string queueName, IClientConnection connection, CancellationToken cancellationToken = default);
    Task<CommandResponse> SubscribeQueueWithBatch(QueueSubscribeWithBatchDto queueSubscribeWithBatchDto, IClientConnection connection, CancellationToken cancellationToken = default);
    
    Task RestoreQueues(CancellationToken cancellationToken = default);
    
    Task<CommandResponse> PublishMessage(string queueName, MambaMessage message, IClientConnection connection, CancellationToken cancellationToken = default);
    Task<CommandResponse> DeleteMessage(string queueName, Guid messageId, IClientConnection connection, CancellationToken cancellationToken = default);
    Task<CommandResponse> DeleteMessages(string queueName, ICollection<Guid> messageIds, IClientConnection connection, CancellationToken cancellationToken = default);
    Task<CommandResponse<GetMessagesResponse>> GetMessagesAsync(string queueName, int skip, int take, CancellationToken cancellationToken = default);
}