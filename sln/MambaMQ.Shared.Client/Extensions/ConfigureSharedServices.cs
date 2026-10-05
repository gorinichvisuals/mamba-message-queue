namespace MambaMQ.Shared.Client.Extensions;

public static class ConfigureSharedServices
{
    public static void AddClientSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IConnection, TcpConnection>();
        services.AddSingleton<IAuthenticateService, AuthenticateService>();
    }
}