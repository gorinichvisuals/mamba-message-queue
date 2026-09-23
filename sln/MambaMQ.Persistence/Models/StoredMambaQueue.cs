namespace MambaMQ.Persistence.Models;

public sealed record StoredMambaQueue(
    Guid Id,
    string Name,
    bool IsDurable,
    bool PersistMessages);