namespace MambaMQ.Persistence.Models;

public sealed record StoredMambaQueueState(
    StoredMambaQueue Queue,
    IReadOnlyCollection<StoredMambaMessage> Messages);