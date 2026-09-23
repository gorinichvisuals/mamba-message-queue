namespace MambaMQ.Client;

public interface IMamba
{
    Task PublishAsync<T>(string queueName, T message, bool isDurable = true, bool persistMessages = false, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MambaMessage> SubscribeAsync(string queueName, bool isDurable = true, bool persistMessages = false, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default);
}