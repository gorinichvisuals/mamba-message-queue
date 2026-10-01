namespace MambaMQ.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static void ConfigurePersistence(
        this IServiceCollection services, 
        string storagePath, 
        int messageSegmentSizeInBytes,
        int maxMessageSegments)
    {
        services.AddSingleton<IServerStorageService>(
            sp => new ServerStorageService(
                sp.GetRequiredService<IFileStorageService>(), 
                messageSegmentSizeInBytes, 
                maxMessageSegments));
        
        services.AddSingleton<IFileStorageService>(_ => new FileStorageService(storagePath));
    }
}