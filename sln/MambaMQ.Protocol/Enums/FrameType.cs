namespace MambaMQ.Protocol.Enums;

public enum FrameType : byte
{
    PublishMessage = 1,
    SubscribeQueue = 2,
    DeleteMessage = 3,
    CreateQueue = 4,
    GetMessage = 5,
    Authentication = 6,
    SubscribeQueueWithBatch = 7,
    BatchMessages = 8,
    ServiceIdentity = 9,
    CreateExchange = 10,
    BindExchange = 11,
    UnbindExchange = 12,
    PublishToExchange = 13,
    CommandResponse = 14,
}