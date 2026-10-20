namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandEncoder
{
    public static byte[] Encode(ICommand command)
    {
        return command.Type switch
        {
            FrameType.Authentication => AuthenticationCommandEncoder.EncodeAuthentication((AuthenticationCommand)command),
            FrameType.ServiceIdentity => AuthenticationCommandEncoder.EncodeServiceIdentity((ServiceIdentityCommand)command),
            
            FrameType.PublishMessage => MessageCommandEncoder.EncodePublishMessage((PublishMessageCommand)command),
            FrameType.DeleteMessage => MessageCommandEncoder.EncodeDeleteMessage((DeleteMessageCommand)command),
            FrameType.DeleteSelectedMessagesCommand => MessageCommandEncoder.EncodeDeleteMessages((DeleteMessagesCommand)command),
            FrameType.GetMessagesCommand => MessageCommandEncoder.EncodeGetMessages((GetMessagesCommand)command),

            FrameType.CreateQueue => QueueCommandEncoder.EncodeCreateQueue((CreateQueueCommand)command),
            FrameType.SubscribeQueue => QueueCommandEncoder.EncodeSubscribeQueue((SubscribeQueueCommand)command),
            FrameType.SubscribeQueueWithBatch => QueueCommandEncoder.EncodeSubscribeQueueWithBatch((SubscribeQueueWithBatchCommand)command),
            FrameType.GetQueuesCommand => QueueCommandEncoder.EncodeGetQueues(),
            FrameType.DeleteQueueCommand => QueueCommandEncoder.EncodeDeleteQueue((DeleteQueueCommand)command),
            FrameType.UpdateQueueCommand => QueueCommandEncoder.EncodeUpdateQueue((UpdateQueueCommand)command),
            
            FrameType.CreateExchange => ExchangeCommandEncoder.EncodeCreateExchange((CreateExchangeCommand)command),
            FrameType.GetExchangesCommand => ExchangeCommandEncoder.EncodeGetExchanges(),
            FrameType.BindExchange => ExchangeCommandEncoder.EncodeBindExchange((BindExchangeCommand)command),
            FrameType.UnbindExchange => ExchangeCommandEncoder.EncodeUnbindExchange((UnbindExchangeCommand)command),
            FrameType.PublishToExchange => ExchangeCommandEncoder.EncodePublishToExchange((PublishToExchangeCommand)command),
            FrameType.DeleteExchangeCommand => ExchangeCommandEncoder.EncodeDeleteExchange((DeleteExchangeCommand)command),
            FrameType.UpdateExchangeCommand => ExchangeCommandEncoder.EncodeUpdateExchange((UpdateExchangeCommand)command),

            _ => throw new ArgumentException($"Unsupported command type: {command.Type}.", nameof(command))
        };
    }
}
