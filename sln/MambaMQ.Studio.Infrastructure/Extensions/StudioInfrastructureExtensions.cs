namespace MambaMQ.Studio.Infrastructure.Extensions;

public static class StudioInfrastructureExtensions
{
    public static void AddStudioInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionProfileService, ConnectionProfileService>();
        services.AddSingleton<IMambaStudioClient, MambaStudioClient>();
        services.AddSingleton<IConnection, TcpConnection>();
        services.AddSingleton<IAuthenticateService, AuthenticateService>();
        services.AddPersistenceServices();
    }
}