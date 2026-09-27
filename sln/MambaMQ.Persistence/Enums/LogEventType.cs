namespace MambaMQ.Persistence.Enums;

public enum LogEventType : byte
{
    Unknown,

    QueueCreated,
    QueueNotCreated,
    QueueAlreadyExists,
    QueueRestored,
    
    MessagePublished,
    MessageSavedToStorage,
    MessageDelivered,
    MessageDeleted,
    MessagesRestored,
    MessageNotSavedToStorage,
    
    ConsumerConnected,
    ConsumerDisconnected
}