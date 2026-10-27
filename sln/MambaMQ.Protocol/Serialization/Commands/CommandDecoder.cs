namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandDecoder
{
    public static ICommand Decode(FrameType type, ReadOnlySpan<byte> buffer)
    {
        return type switch
        {
            FrameType.ConnectionPingCommand => new ConnectionPingCommand(),
            FrameType.ServiceAuthenticateCommand => AuthenticationCommandDecoder.DecodeServiceAuthentication(buffer),
            FrameType.ManagementAuthenticateCommand => AuthenticationCommandDecoder.DecodeManagementAuthentication(buffer),
            FrameType.ServiceIdentitySetCommand => AuthenticationCommandDecoder.DecodeServiceIdentity(buffer),

            FrameType.QueueMessagePublishCommand => MessageCommandDecoder.DecodePublish(buffer),
            FrameType.QueueMessageDeleteCommand => MessageCommandDecoder.DecodeDelete(buffer),
            FrameType.QueueMessagesDeleteCommand => MessageCommandDecoder.DecodeDeleteMessages(buffer),
            FrameType.QueueMessagesGetCommand => MessageCommandDecoder.DecodeGetMessages(buffer),

            FrameType.QueueCreateCommand => QueueCommandDecoder.DecodeCreateQueue(buffer),
            FrameType.QueueSubscribeCommand => QueueCommandDecoder.DecodeSubscribeQueue(buffer),
            FrameType.QueueSubscribeBatchCommand => QueueCommandDecoder.DecodeSubscribeQueueWithBatch(buffer),
            FrameType.QueueGetListCommand => QueueCommandDecoder.DecodeGetQueues(),
            FrameType.QueueDeleteCommand => QueueCommandDecoder.DecodeDeleteQueue(buffer),
            FrameType.QueueUpdateCommand => QueueCommandDecoder.DecodeUpdateQueue(buffer),

            FrameType.ExchangeCreateCommand => ExchangeCommandDecoder.DecodeCreateExchange(buffer),
            FrameType.ExchangeGetListCommand => ExchangeCommandDecoder.DecodeGetExchanges(),
            FrameType.ExchangeBindCommand => ExchangeCommandDecoder.DecodeBindExchange(buffer),
            FrameType.ExchangeUnbindCommand => ExchangeCommandDecoder.DecodeUnbindExchange(buffer),
            FrameType.ExchangeMessagePublishCommand => ExchangeCommandDecoder.DecodePublishToExchange(buffer),
            FrameType.ExchangeDeleteCommand => ExchangeCommandDecoder.DecodeDeleteExchange(buffer),
            FrameType.ExchangeUpdateCommand => ExchangeCommandDecoder.DecodeUpdateExchange(buffer),

            _ => throw new InvalidDataException($"Unsupported frame type: {type}.")
        };
    }
}