namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class ServerStorageService(
    IFileStorageService fileStorage, 
    int messageSegmentSizeInBytes,
    int maxMessageSegments,
    int queueLogSegmentSizeInBytes,
    int serverLogSegmentSizeInBytes) : IServerStorageService
{
    private const byte StorageVersion = 1;

    private const string LogsDirectory = "logs";
    private const string ServerDirectory = "server";
    private const string MessagesDirectory = "messages";
    private const string QueuesDirectory = "queues";

    private const string SegmentPrefix = "segment-";
    private const string SegmentExtension = ".dat";
    private const string QueueMetadataFile = "queue.dat";

    private const int MessageRecordLengthSize = sizeof(int);
    private const int MessageRecordTypeSize = sizeof(byte);
    private const int MessageIdSize = 16;
    private const int MessageReceivedAtSize = sizeof(long);
    private const int MessageBodyLengthSize = sizeof(int);
    
    private const int QueueLogTimestampSize = sizeof(long);
    private const int QueueLogLevelSize = sizeof(byte);
    private const int QueueLogEventTypeSize = sizeof(byte);
    private const int QueueLogMessageLengthSize = sizeof(int);
    
    private readonly ConcurrentDictionary<Guid, QueueStorageState> _states = [];

    private readonly int _messageSegmentSizeInBytes = messageSegmentSizeInBytes > 0
        ? messageSegmentSizeInBytes
        : throw new ArgumentOutOfRangeException(nameof(messageSegmentSizeInBytes));

    private readonly int _maxMessageSegments = maxMessageSegments > 0
        ? maxMessageSegments
        : throw new ArgumentOutOfRangeException(nameof(maxMessageSegments));
    
    private readonly int _queueLogSegmentSizeInBytes = queueLogSegmentSizeInBytes > 0
        ? queueLogSegmentSizeInBytes
        : throw new ArgumentOutOfRangeException(nameof(queueLogSegmentSizeInBytes));
    
    private readonly int _serverLogSegmentSizeInBytes = serverLogSegmentSizeInBytes > 0
        ? serverLogSegmentSizeInBytes
        : throw new ArgumentOutOfRangeException(nameof(serverLogSegmentSizeInBytes));
}