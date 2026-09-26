namespace MambaMQ.Persistence.Models;

public sealed record StoredQueueLog(
    DateTimeOffset Timestamp,
    byte Level,
    byte EventType,
    string Message);