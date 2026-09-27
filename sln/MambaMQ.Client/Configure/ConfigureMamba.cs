namespace MambaMQ.Client.Configure;

public static class ConfigureMamba
{
    public static void AddMamba(this IServiceCollection services, Action<MambaClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        MambaClientOptions options = new();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<TcpConnection>();

        services.AddSingleton<IConnection>(serviceProvider =>
            new AuthenticatedConnection(
                serviceProvider.GetRequiredService<TcpConnection>(), 
                serviceProvider.GetRequiredService<MambaClientOptions>()));
        
        services.AddSingleton<IMamba, MambaClient>();
    }
}