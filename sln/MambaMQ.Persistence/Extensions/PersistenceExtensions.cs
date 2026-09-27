namespace MambaMQ.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static void ConfigurePersistence(
        this IServiceCollection services, 
        string storagePath, 
        int messageSegmentSizeInBytes,
        int maxMessageSegments,
        int queuelogsSegmentSizeInBytes,
        int serverLogsegmentSizeInBytes)
    {
        services.AddSingleton<IServerStorageService>(
            sp => new ServerStorageService(
                sp.GetRequiredService<IFileStorageService>(), 
                messageSegmentSizeInBytes, 
                maxMessageSegments, 
                queuelogsSegmentSizeInBytes,
                serverLogsegmentSizeInBytes));
        
        services.AddSingleton<IFileStorageService>(_ => new FileStorageService(storagePath));
    }
}