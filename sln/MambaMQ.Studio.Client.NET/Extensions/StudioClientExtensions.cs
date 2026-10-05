namespace MambaMQ.Studio.Client.NET.Extensions;

public static class StudioClientExtensions
{
    public static void AddStudioClientServices(this IServiceCollection services)
    {
        services.AddSingleton<IMambaStudioClient, MambaStudioClient>();
        services.AddClientSharedServices();
    }
}