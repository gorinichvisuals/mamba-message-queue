namespace MambaMQ.Protocol.Enums;

public enum FrameType : byte
{
    ClientAuthenticateCommand = 1,
    ServiceIdentitySetCommand = 2,

    ConnectionPingCommand = 3,
    ConnectionPongResponse = 4,
    CommandResultResponse = 5,

    QueueCreateCommand = 6,
    QueueGetCommand = 7,
    QueueGetResponse = 8,
    QueueUpdateCommand = 9,
    QueueDeleteCommand = 10,
    QueueSubscribeCommand = 11,
    QueueSubscribeBatchCommand = 12,
    QueueMessagePublishCommand = 13,
    QueueMessageDeleteCommand = 14,
    QueueMessageDeliverResponse = 15,
    QueueMessagesGetCommand = 16,
    QueueMessagesGetResponse = 17,
    QueueMessagesDeleteCommand = 18,
    QueueMessagesBatchResponse = 19,

    ExchangeCreateCommand = 20,
    ExchangeGetCommand = 21,
    ExchangeGetResponse = 22,
    ExchangeUpdateCommand = 23,
    ExchangeDeleteCommand = 24,
    ExchangeBindCommand = 25,
    ExchangeUnbindCommand = 26,
    ExchangeMessagePublishCommand = 27,
}