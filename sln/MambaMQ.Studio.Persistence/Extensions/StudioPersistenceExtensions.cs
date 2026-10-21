namespace MambaMQ.Studio.Persistence.Extensions;

public static class StudioPersistenceExtensions
{
    public static void AddPersistenceServices(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionProfileStorage, JsonConnectionProfileStorage>();
    } 
}