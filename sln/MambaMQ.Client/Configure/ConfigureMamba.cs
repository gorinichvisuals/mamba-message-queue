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
        services.AddSingleton<IConnection, TcpConnection>();
        services.AddSingleton<IMamba, MambaClient>();
    }
}