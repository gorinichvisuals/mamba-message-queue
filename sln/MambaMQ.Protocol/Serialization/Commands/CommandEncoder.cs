namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandEncoder
{
    public static byte[] Encode(ICommand command)
    {
        return command.Type switch
        {
            FrameType.ConnectionPingCommand => [],
            FrameType.ServiceAuthenticateCommand => AuthenticationCommandEncoder.EncodeServiceAuthentication((ServiceAuthenticateCommand)command),
            FrameType.ManagementAuthenticateCommand => AuthenticationCommandEncoder.EncodeManagementAuthentication((ManagementAuthenticateCommand)command),
            FrameType.ServiceIdentitySetCommand => AuthenticationCommandEncoder.EncodeServiceIdentity((ServiceIdentitySetCommand)command),

            FrameType.QueueMessagePublishCommand => MessageCommandEncoder.EncodePublishMessage((QueueMessagePublishCommand)command),
            FrameType.QueueMessageDeleteCommand => MessageCommandEncoder.EncodeDeleteMessage((QueueMessageDeleteCommand)command),
            FrameType.QueueMessagesDeleteCommand => MessageCommandEncoder.EncodeDeleteMessages((QueueMessagesDeleteCommand)command),
            FrameType.QueueMessagesGetCommand => MessageCommandEncoder.EncodeGetMessages((QueueMessagesGetCommand)command),

            FrameType.QueueCreateCommand => QueueCommandEncoder.EncodeCreateQueue((QueueCreateCommand)command),
            FrameType.QueueSubscribeCommand => QueueCommandEncoder.EncodeSubscribeQueue((QueueSubscribeCommand)command),
            FrameType.QueueSubscribeBatchCommand => QueueCommandEncoder.EncodeSubscribeQueueWithBatch((QueueSubscribeBatchCommand)command),
            FrameType.QueueGetListCommand => QueueCommandEncoder.EncodeGetQueues(),
            FrameType.QueueDeleteCommand => QueueCommandEncoder.EncodeDeleteQueue((QueueDeleteCommand)command),
            FrameType.QueueUpdateCommand => QueueCommandEncoder.EncodeUpdateQueue((QueueUpdateCommand)command),

            FrameType.ExchangeCreateCommand => ExchangeCommandEncoder.EncodeCreateExchange((ExchangeCreateCommand)command),
            FrameType.ExchangeGetListCommand => ExchangeCommandEncoder.EncodeGetExchanges(),
            FrameType.ExchangeBindCommand => ExchangeCommandEncoder.EncodeBindExchange((ExchangeBindCommand)command),
            FrameType.ExchangeUnbindCommand => ExchangeCommandEncoder.EncodeUnbindExchange((ExchangeUnbindCommand)command),
            FrameType.ExchangeMessagePublishCommand => ExchangeCommandEncoder.EncodePublishToExchange((ExchangeMessagePublishCommand)command),
            FrameType.ExchangeDeleteCommand => ExchangeCommandEncoder.EncodeDeleteExchange((ExchangeDeleteCommand)command),
            FrameType.ExchangeUpdateCommand => ExchangeCommandEncoder.EncodeUpdateExchange((ExchangeUpdateCommand)command),

            _ => throw new ArgumentException($"Unsupported command type: {command.Type}.", nameof(command))
        };
    }
}