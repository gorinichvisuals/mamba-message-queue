namespace MambaMQ.Persistence.Enums;

public enum LogEventType : byte
{
    Unknown,

    QueueCreated,
    QueueAlreadyExists,
    QueueRestored,
    
    MessagePublished,
    MessageSavedToStorage,
    MessageDelivered,
    MessageDeleted,
    MessagesRestored,
    
    ConsumerConnected,
    ConsumerDisconnected
}