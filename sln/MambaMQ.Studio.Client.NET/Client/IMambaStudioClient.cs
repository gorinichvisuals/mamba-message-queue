namespace MambaMQ.Studio.Client.NET.Client;

public interface IMambaStudioClient
{
    Task ConnectAsync(string host, int port, string username, string password, CancellationToken cancellationToken = default);
    ValueTask DisconnectAsync(CancellationToken cancellationToken = default);
    Task<GetQueuesResponse> GetQueuesAsync(CancellationToken cancellationToken = default);
    Task<CommandResponse<QueueModel>> CreateQueueAsync(QueueOptions options, CancellationToken cancellationToken = default);
    Task<CommandResponse> DeleteQueueAsync(Guid queueId, CancellationToken cancellationToken = default);
    Task<CommandResponse<QueueModel>> UpdateQueueAsync(Guid queueId, QueueOptions options, CancellationToken cancellationToken = default);
}