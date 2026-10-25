namespace MambaMQ.Studio.Infrastructure.Extensions;

using MambaMQ.Studio.Infrastructure.Client.Factories;

public static class StudioInfrastructureExtensions
{
    public static void AddStudioInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionProfileService, ConnectionProfileService>();
        services.AddSingleton<IMambaStudioClientFactory, MambaStudioClientFactory>();
        services.AddPersistenceServices();
    }
}