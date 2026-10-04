namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class ServerStorageService
{
    public async Task SaveExchange(StoredMambaExchange storedExchange, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storedExchange);

        byte[] data = SerializeExchange(storedExchange);

        await fileStorage.ReplaceFile(GetExchangeMetadataPath(storedExchange.Id), data, flushToDisk: true, cancellationToken);
    }

    public async Task<IReadOnlyCollection<StoredMambaExchange>> RestoreExchanges(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> directories = await fileStorage.GetDirectories(ExchangesDirectory, cancellationToken);

        List<StoredMambaExchange> exchanges = [];

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Guid.TryParseExact(directory, "N", out Guid exchangeId))
                continue;

            StoredMambaExchange exchange = await RestoreExchangeMetadata(exchangeId, cancellationToken);

            exchanges.Add(exchange);
        }

        return exchanges;
    }

    public async Task DeleteExchange(Guid exchangeId, CancellationToken cancellationToken = default)
    {
        await fileStorage.DeleteDirectory(GetExchangePath(exchangeId), cancellationToken);
    }

    private async Task<StoredMambaExchange> RestoreExchangeMetadata(Guid exchangeId, CancellationToken cancellationToken)
    {
        string path = GetExchangeMetadataPath(exchangeId);

        await using Stream stream = await fileStorage.OpenReadFile(path);

        StoredMambaExchange storedExchange = await DeserializeExchange(stream, cancellationToken);

        return storedExchange.Id != exchangeId
            ? throw new InvalidDataException($"Exchange metadata ID '{storedExchange.Id}' does not match directory ID '{exchangeId}'.")
            : storedExchange;
    }
    
    private static byte[] SerializeExchange(StoredMambaExchange exchange)
    {
        byte[] name = Encoding.UTF8.GetBytes(exchange.Name);

        int permissionsSize = sizeof(int) + (
            from permission in exchange.Permissions
            let serviceName = Encoding.UTF8.GetBytes(permission.Key)
            select sizeof(int) + serviceName.Length + sizeof(byte)
        ).Sum();

        int bindingsSize = sizeof(int) + (
            from binding in exchange.Bindings
            let queueName = Encoding.UTF8.GetBytes(binding.QueueName)
            let routingKey = Encoding.UTF8.GetBytes(binding.RoutingKey)
            select sizeof(int) + queueName.Length +
                   sizeof(int) + routingKey.Length
        ).Sum();

        int size =
            sizeof(byte) +
            16 +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(int) +
            name.Length +
            permissionsSize +
            bindingsSize;

        byte[] buffer = new byte[size];

        Span<byte> span = buffer;

        int offset = 0;

        span[offset++] = StorageVersion;

        exchange.Id.TryWriteBytes(span[offset..]);
        offset += 16;

        span[offset++] = exchange.IsDurable
            ? (byte)1
            : (byte)0;

        span[offset++] = exchange.Type;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], name.Length);

        offset += sizeof(int);

        name.CopyTo(span[offset..]);
        offset += name.Length;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], exchange.Permissions.Count);

        offset += sizeof(int);

        foreach ((string serviceName, byte permission) in exchange.Permissions)
        {
            byte[] serviceNameBytes = Encoding.UTF8.GetBytes(serviceName);

            BinaryPrimitives.WriteInt32BigEndian(span[offset..], serviceNameBytes.Length);

            offset += sizeof(int);

            serviceNameBytes.CopyTo(span[offset..]);
            offset += serviceNameBytes.Length;

            span[offset++] = permission;
        }

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], exchange.Bindings.Count);

        offset += sizeof(int);

        foreach (StoredExchangeBinding binding in exchange.Bindings)
        {
            byte[] queueName = Encoding.UTF8.GetBytes(binding.QueueName);
            byte[] routingKey = Encoding.UTF8.GetBytes(binding.RoutingKey);

            BinaryPrimitives.WriteInt32BigEndian(span[offset..], queueName.Length);

            offset += sizeof(int);

            queueName.CopyTo(span[offset..]);
            offset += queueName.Length;

            BinaryPrimitives.WriteInt32BigEndian(span[offset..], routingKey.Length);

            offset += sizeof(int);

            routingKey.CopyTo(span[offset..]);
            offset += routingKey.Length;
        }

        return buffer;
    }
        
    private static async Task<StoredMambaExchange> DeserializeExchange(
        Stream stream,
        CancellationToken cancellationToken)
    {
        const int headerSize =
            sizeof(byte) +
            16 +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(int);

        byte[] header = new byte[headerSize];

        await ReadExactly(stream, header, cancellationToken);

        ReadOnlySpan<byte> span = header;

        int offset = 0;

        byte version = span[offset++];

        if (version is not StorageVersion)
            throw new InvalidDataException($"Unsupported storage version '{version}'.");

        Guid exchangeId = new(span.Slice(offset, 16));

        offset += 16;

        bool isDurable = span[offset++] is not 0;

        byte type = span[offset++];

        int nameLength = BinaryPrimitives.ReadInt32BigEndian(span[offset..]);

        if (nameLength <= 0)
            throw new InvalidDataException("Exchange name length must be greater than zero.");

        byte[] nameBuffer = new byte[nameLength];

        await ReadExactly(stream, nameBuffer, cancellationToken);

        string name = Encoding.UTF8.GetString(nameBuffer);

        byte[] countBuffer = new byte[sizeof(int)];

        await ReadExactly(stream, countBuffer, cancellationToken);

        int permissionsCount = BinaryPrimitives.ReadInt32BigEndian(countBuffer);

        if (permissionsCount < 0)
            throw new InvalidDataException("Exchange permissions count cannot be negative.");

        Dictionary<string, byte> permissions = new(permissionsCount);

        for (int i = 0; i < permissionsCount; i++)
        {
            await ReadExactly(stream, countBuffer, cancellationToken);

            int serviceNameLength = BinaryPrimitives.ReadInt32BigEndian(countBuffer);

            if (serviceNameLength <= 0)
                throw new InvalidDataException("Service name length must be greater than zero.");

            byte[] serviceNameBuffer = new byte[serviceNameLength];

            await ReadExactly(stream, serviceNameBuffer, cancellationToken);

            string serviceName = Encoding.UTF8.GetString(serviceNameBuffer);

            int permission = stream.ReadByte();

            if (permission < 0)
                throw new EndOfStreamException();

            permissions.Add(serviceName, (byte)permission);
        }

        await ReadExactly(stream, countBuffer, cancellationToken);

        int bindingsCount = BinaryPrimitives.ReadInt32BigEndian(countBuffer);

        if (bindingsCount < 0)
            throw new InvalidDataException("Exchange bindings count cannot be negative.");

        List<StoredExchangeBinding> bindings = new(bindingsCount);

        for (int i = 0; i < bindingsCount; i++)
        {
            await ReadExactly(stream, countBuffer, cancellationToken);

            int queueNameLength = BinaryPrimitives.ReadInt32BigEndian(countBuffer);

            if (queueNameLength <= 0)
                throw new InvalidDataException("Queue name length must be greater than zero.");

            byte[] queueNameBuffer = new byte[queueNameLength];

            await ReadExactly(stream, queueNameBuffer, cancellationToken);

            string queueName = Encoding.UTF8.GetString(queueNameBuffer);

            await ReadExactly(stream, countBuffer, cancellationToken);

            int routingKeyLength = BinaryPrimitives.ReadInt32BigEndian(countBuffer);

            if (routingKeyLength < 0)
                throw new InvalidDataException("Routing key length cannot be negative.");

            byte[] routingKeyBuffer = new byte[routingKeyLength];

            await ReadExactly(stream, routingKeyBuffer, cancellationToken);

            string routingKey = Encoding.UTF8.GetString(routingKeyBuffer);

            bindings.Add(new StoredExchangeBinding(queueName, routingKey));
        }

        return new StoredMambaExchange(
            exchangeId,
            name,
            isDurable,
            type,
            permissions,
            bindings);
    }
        
    private static string GetExchangePath(Guid exchangeId)
        => $"{ExchangesDirectory}/{exchangeId:N}";

    private static string GetExchangeMetadataPath(Guid exchangeId)
        => $"{GetExchangePath(exchangeId)}/{ExchangeMetadataFile}";
}