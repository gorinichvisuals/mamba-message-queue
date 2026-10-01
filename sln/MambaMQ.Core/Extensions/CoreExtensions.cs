namespace MambaMQ.Core.Extensions;

public static class CoreExtensions
{
    public static void AddCoreExtensions(this IServiceCollection services)
    {
        services.AddSingleton<IQueueManager, QueueManager>();
    }
}