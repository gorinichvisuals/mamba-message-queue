namespace MambaMQ.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static void ConfigurePersistence(
        this IServiceCollection services, 
        string storagePath, 
        int messageSegmentSizeInBytes,
        int maxMessageSegments,
        int logSegmentSizeInBytes)
    {
        services.AddSingleton<IQueueStorageService>(
            sp => new QueueStorageService(
                sp.GetRequiredService<IFileStorageService>(), 
                messageSegmentSizeInBytes, 
                maxMessageSegments, 
                logSegmentSizeInBytes));
        
        services.AddSingleton<IFileStorageService>(_ => new FileStorageService(storagePath));
    }
}