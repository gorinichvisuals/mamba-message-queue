namespace MambaMQ.Persistence.Models;

public sealed record StoredMambaMessage(
    Guid Id,
    DateTimeOffset ReceivedAt,
    ReadOnlyMemory<byte> Body);