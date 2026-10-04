namespace MambaMQ.Core.Extensions;

public static class CoreExtensions
{
    public static void AddCoreExtensions(this IServiceCollection services, bool authorizationEnabled)
    {
        services.AddSingleton<IExchangeManager>(sp => 
            new ExchangeManager(authorizationEnabled,
                sp.GetRequiredService<IServerStorageService>(),
                sp.GetRequiredService<IMambaLogger>()));
        
        services.AddSingleton<IQueueManager>(sp =>
            new QueueManager(
                authorizationEnabled,
                sp.GetRequiredService<IServerStorageService>(),
                sp.GetRequiredService<IMambaLogger>()));
    }
}