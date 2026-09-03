namespace MambaMQ.Client;

public interface IMamba
{
    Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MambaMessage> SubscribeAsync(string queueName, bool autoAcknowledge = true, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default);
}