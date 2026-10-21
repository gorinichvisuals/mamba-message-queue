namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandDecoder
{
    public static ICommand Decode(FrameType type, ReadOnlySpan<byte> buffer)
    {
        return type switch
        {
            FrameType.PingCommand => new PingCommand(),
            FrameType.Authentication => AuthenticationCommandDecoder.DecodeAuthentication(buffer),
            FrameType.ServiceIdentity => AuthenticationCommandDecoder.DecodeServiceIdentity(buffer),
            
            FrameType.PublishMessage => MessageCommandDecoder.DecodePublish(buffer),
            FrameType.DeleteMessage => MessageCommandDecoder.DecodeDelete(buffer),
            FrameType.DeleteSelectedMessagesCommand => MessageCommandDecoder.DecodeDeleteMessages(buffer),
            FrameType.GetMessagesCommand => MessageCommandDecoder.DecodeGetMessages(buffer),
            
            FrameType.CreateQueue => QueueCommandDecoder.DecodeCreateQueue(buffer),
            FrameType.SubscribeQueue => QueueCommandDecoder.DecodeSubscribeQueue(buffer),
            FrameType.SubscribeQueueWithBatch => QueueCommandDecoder.DecodeSubscribeQueueWithBatch(buffer),
            FrameType.GetQueuesCommand => QueueCommandDecoder.DecodeGetQueues(),
            FrameType.DeleteQueueCommand => QueueCommandDecoder.DecodeDeleteQueue(buffer),
            FrameType.UpdateQueueCommand => QueueCommandDecoder.DecodeUpdateQueue(buffer),
            
            FrameType.CreateExchange => ExchangeCommandDecoder.DecodeCreateExchange(buffer),
            FrameType.GetExchangesCommand => ExchangeCommandDecoder.DecodeGetExchanges(),
            FrameType.BindExchange => ExchangeCommandDecoder.DecodeBindExchange(buffer),
            FrameType.UnbindExchange => ExchangeCommandDecoder.DecodeUnbindExchange(buffer),
            FrameType.PublishToExchange => ExchangeCommandDecoder.DecodePublishToExchange(buffer),
            FrameType.DeleteExchangeCommand => ExchangeCommandDecoder.DecodeDeleteExchange(buffer),
            FrameType.UpdateExchangeCommand => ExchangeCommandDecoder.DecodeUpdateExchange(buffer),
            
            _ => throw new InvalidDataException($"Unsupported frame type: {type}.")
        };
    }
}
