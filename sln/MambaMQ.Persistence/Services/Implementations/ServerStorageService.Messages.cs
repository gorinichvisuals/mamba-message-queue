namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class ServerStorageService
{
   public async Task SaveMessage(Guid queueId, StoredMambaMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        QueueStorageState state = GetOrCreateState(queueId);

        await state.Gate.WaitAsync(cancellationToken);

        try
        {
            byte[] record = SerializeMessage(message);

            await AppendRecord(queueId, state, record, cancellationToken);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    public async Task MarkAsDeleteMessage(Guid queueId, Guid messageId, CancellationToken cancellationToken = default)
    {
        QueueStorageState state = GetOrCreateState(queueId);

        await state.Gate.WaitAsync(cancellationToken);

        try
        {
            byte[] record = SerializeDelete(messageId);

            await AppendRecord(queueId, state, record, cancellationToken);
        }
        finally
        {
            state.Gate.Release();
        }
    }
    
    public async Task CleanupMessages(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> directories = await fileStorage.GetDirectories(QueuesDirectory, cancellationToken);

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Guid.TryParseExact(directory, "N", out Guid queueId))
                continue;

            QueueStorageState state = GetOrCreateState(queueId);

            await state.Gate.WaitAsync(cancellationToken);

            try
            {
                StoredMambaQueue queue = await RestoreQueueMetadata(queueId, cancellationToken);

                if (!queue.MessageRetention.RetentionEnabled)
                    continue;

                await CleanupMessagesForQueue(queueId, queue.MessageRetention.RetentionPeriod, cancellationToken);
            }
            finally
            {
                state.Gate.Release();
            }
        }
    }
    
    private async Task CleanupMessagesForQueue(
        Guid queueId,
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> files = await fileStorage.GetFiles(GetMessagesPath(queueId), cancellationToken);

        List<(int Number, string Path)> segments = files
            .Select(ParseSegment)
            .Where(static segment => segment is not null)
            .Select(static segment => segment!.Value)
            .OrderBy(static segment => segment.Number)
            .ToList();

        if (segments.Count <= _maxMessageSegments)
            return;

        int activeSegment = segments[^1].Number;

        DateTimeOffset cutoff =
            DateTimeOffset.UtcNow - retentionPeriod;

        Dictionary<int, HashSet<Guid>> messagesBySegment = [];
        Dictionary<Guid, int> deleteSegments = [];

        foreach ((int number, string path) in segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using Stream stream = await fileStorage.OpenReadFile(path);

            await AnalyzeSegment(stream, number, messagesBySegment, deleteSegments, cancellationToken);
        }

        foreach ((int number, string path) in segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (number == activeSegment)
                continue;

            if (!messagesBySegment.TryGetValue(number, out HashSet<Guid>? messageIds))
                continue;

            bool allMessagesDeleted = messageIds.All(messageId => deleteSegments.TryGetValue(messageId, out int deleteSegment) && deleteSegment >= number);

            if (!allMessagesDeleted)
                continue;

            DateTime lastWriteTimeUtc = fileStorage.GetLastWriteTimeUtc(path);;

            if (lastWriteTimeUtc >= cutoff.UtcDateTime)
                continue;

            await fileStorage.DeleteFile(path);
        }
    }
    
    private static async Task AnalyzeSegment(
        Stream stream,
        int segmentNumber,
        Dictionary<int, HashSet<Guid>> messagesBySegment,
        Dictionary<Guid, int> deleteSegments,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> messageIds = [];

        messagesBySegment[segmentNumber] = messageIds;

        byte[] lengthBuffer = new byte[MessageRecordLengthSize];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int bytesRead = await ReadAtMost(stream, lengthBuffer, cancellationToken);

            switch (bytesRead)
            {
                case 0:
                case < MessageRecordLengthSize:
                    return;
            }

            int recordLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

            if (recordLength <= 0)
                throw new InvalidDataException($"Invalid record length '{recordLength}'.");

            byte[] record = new byte[recordLength];

            bytesRead = await ReadAtMost(stream, record, cancellationToken);

            if (bytesRead < recordLength)
                return;

            AnalyzeRecord(record, messageIds, segmentNumber, deleteSegments);
        }
    }
    
    private static void AnalyzeRecord(
        ReadOnlySpan<byte> record,
        HashSet<Guid> messageIds,
        int segmentNumber,
        Dictionary<Guid, int> deleteSegments)
    {
        if (record.Length < MessageRecordTypeSize)
            throw new InvalidDataException("Storage record is too small.");

        int offset = 0;

        StorageRecordType recordType = (StorageRecordType)record[offset++];

        switch (recordType)
        {
            case StorageRecordType.Message:
            {
                const int minimumLength = MessageRecordTypeSize + MessageIdSize + MessageReceivedAtSize + MessageBodyLengthSize;

                if (record.Length < minimumLength)
                    throw new InvalidDataException("Message record is too small.");

                Guid messageId = new(record.Slice(offset, MessageIdSize));

                messageIds.Add(messageId);

                break;
            }

            case StorageRecordType.DeleteMessage:
            {
                const int expectedLength = MessageRecordTypeSize + MessageIdSize;

                if (record.Length != expectedLength)
                    throw new InvalidDataException("Invalid delete message record length.");

                Guid messageId = new(record.Slice(offset, MessageIdSize));

                deleteSegments[messageId] = segmentNumber;

                break;
            }

            default:
                throw new InvalidDataException($"Unknown storage record type '{recordType}'.");
        }
    }
    
    private async Task AppendRecord(Guid queueId, QueueStorageState state, byte[] record, CancellationToken cancellationToken)
    {
        if (state.CurrentSegmentSize > 0 && state.CurrentSegmentSize + record.Length > _messageSegmentSizeInBytes)
        {
            state.CurrentSegment++;
            state.CurrentSegmentSize = 0;
        }

        string path = GetSegmentPath(queueId, state.CurrentSegment);

        await fileStorage.AppendToFile(path, record, flushToDisk: true, cancellationToken);

        state.CurrentSegmentSize += record.Length;
    }
    
    private static byte[] SerializeMessage( StoredMambaMessage message)
    {
        int recordLength = MessageRecordTypeSize + MessageIdSize + MessageReceivedAtSize + MessageBodyLengthSize + message.Body.Length;

        byte[] buffer = new byte[MessageRecordLengthSize + recordLength];

        Span<byte> span = buffer;

        int offset = 0;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], recordLength);

        offset += MessageRecordLengthSize;

        span[offset++] = (byte)StorageRecordType.Message;

        message.Id.TryWriteBytes(span[offset..]);

        offset += MessageIdSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], message.ReceivedAt.UtcTicks);

        offset += MessageReceivedAtSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], message.Body.Length);

        offset += MessageBodyLengthSize;

        message.Body.Span.CopyTo(span[offset..]);

        return buffer;
    }
    
    private static byte[] SerializeDelete(Guid messageId)
    {
        const int recordLength = MessageRecordTypeSize + MessageIdSize;

        byte[] buffer = new byte[MessageRecordLengthSize + recordLength];

        Span<byte> span = buffer;

        int offset = 0;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], recordLength);

        offset += MessageRecordLengthSize;

        span[offset++] = (byte)StorageRecordType.DeleteMessage;

        messageId.TryWriteBytes(span[offset..]);

        return buffer;
    }
    
    private static async Task ReplaySegment(
        Stream stream,
        Dictionary<Guid, StoredMambaMessage> messages,
        List<Guid> messageOrder,
        CancellationToken cancellationToken)
    {
        byte[] lengthBuffer = new byte[MessageRecordLengthSize];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int bytesRead = await ReadAtMost(stream, lengthBuffer, cancellationToken);

            switch (bytesRead)
            {
                case 0:
                case < MessageRecordLengthSize:
                    return;
            }

            int recordLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

            if (recordLength <= 0)
                throw new InvalidDataException($"Invalid record length '{recordLength}'.");

            byte[] record = new byte[recordLength];

            bytesRead = await ReadAtMost(stream, record, cancellationToken);

            if (bytesRead < recordLength)
                return;

            ParseRecord(record, messages, messageOrder);
        }
    }
    
    private static void ParseRecord(
        ReadOnlySpan<byte> record,
        Dictionary<Guid, StoredMambaMessage> messages,
        List<Guid> messageOrder)
    {
        if (record.Length < MessageRecordTypeSize)
            throw new InvalidDataException("Storage record is too small.");

        int offset = 0;

        StorageRecordType recordType = (StorageRecordType)record[offset++];

        switch (recordType)
        {
            case StorageRecordType.Message:
                ParseMessageRecord(record, ref offset, messages, messageOrder);
                break;

            case StorageRecordType.DeleteMessage:
                ParseDeleteRecord(record, ref offset, messages);
                break;

            default:
                throw new InvalidDataException(
                    $"Unknown storage record type '{recordType}'.");
        }
    }
    
    private static void ParseMessageRecord(
        ReadOnlySpan<byte> record,
        ref int offset,
        Dictionary<Guid, StoredMambaMessage> messages,
        List<Guid> messageOrder)
    {
        const int minimumLength = MessageRecordTypeSize + MessageIdSize + MessageReceivedAtSize + MessageBodyLengthSize;

        if (record.Length < minimumLength)
            throw new InvalidDataException("Message record is too small.");

        Guid messageId = new(record.Slice(offset, MessageIdSize));

        offset += MessageIdSize;

        long receivedAtTicks = BinaryPrimitives.ReadInt64BigEndian(record[offset..]);

        offset += MessageReceivedAtSize;

        int bodyLength = BinaryPrimitives.ReadInt32BigEndian(record[offset..]);

        offset += MessageBodyLengthSize;

        if (bodyLength < 0 || bodyLength > record.Length - offset)
            throw new InvalidDataException("Invalid message body length.");

        byte[] body = record
            .Slice(offset, bodyLength)
            .ToArray();

        StoredMambaMessage message = new(messageId, new DateTimeOffset(receivedAtTicks, TimeSpan.Zero), body);

        if (!messages.ContainsKey(messageId))
            messageOrder.Add(messageId);

        messages[messageId] = message;
    }
    
    private static void ParseDeleteRecord(ReadOnlySpan<byte> record, ref int offset, Dictionary<Guid, StoredMambaMessage> messages)
    {
        const int expectedLength = MessageRecordTypeSize + MessageIdSize;

        if (record.Length != expectedLength)
            throw new InvalidDataException("Invalid delete message record length.");

        Guid messageId = new(record.Slice(offset, MessageIdSize));

        messages.Remove(messageId);
    }
    
    private static (int Number, string Path)? ParseSegment(string path)
    {
        string fileName = Path.GetFileName(path);

        if (!fileName.StartsWith(SegmentPrefix, StringComparison.OrdinalIgnoreCase) || !fileName.EndsWith(SegmentExtension, StringComparison.OrdinalIgnoreCase))
            return null;

        string numberText = fileName[SegmentPrefix.Length.. ^SegmentExtension.Length];

        if (!int.TryParse(numberText, out int number))
            return null;

        return (number, path);
    }
    
    private static async Task<int> ReadAtMost(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;

        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], cancellationToken);

            if (read is 0)
                break;

            totalRead += read;
        }

        return totalRead;
    }
    
    private static async Task ReadExactly(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;

        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], cancellationToken);

            if (read is 0)
                throw new EndOfStreamException("Unexpected end of storage file.");

            totalRead += read;
        }
    }
    
    private QueueStorageState GetOrCreateState(Guid queueId)
        => _states.GetOrAdd(queueId, static _ => new QueueStorageState());

    private QueueStorageState? GetState(Guid queueId)
        => _states.GetValueOrDefault(queueId);

    private void SetState(Guid queueId, QueueStorageState state)
        => _states[queueId] = state;

    private void RemoveState(Guid queueId)
        => _states.TryRemove(queueId, out _);
    
    private static string GetMessagesPath(Guid queueId)
        => $"{GetQueuePath(queueId)}/{MessagesDirectory}";
    
    private static string GetSegmentPath(Guid queueId, int segment)
        => $"{GetMessagesPath(queueId)}/{SegmentPrefix}{segment:D6}{SegmentExtension}";
}