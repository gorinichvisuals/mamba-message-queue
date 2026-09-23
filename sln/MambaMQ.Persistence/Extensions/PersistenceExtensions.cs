namespace MambaMQ.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static void ConfigurePersistence(
        this IServiceCollection services, 
        string storagePath, 
        int segmentSizeInBytes,
        int maxSegments)
    {
        services.AddSingleton<IQueueStorageService>(
            sp => new QueueStorageService(sp.GetRequiredService<IFileStorageService>(), segmentSizeInBytes, maxSegments));
        
        services.AddSingleton<IFileStorageService>(_ => new FileStorageService(storagePath));
    }
}