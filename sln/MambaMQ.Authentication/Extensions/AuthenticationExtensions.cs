namespace MambaMQ.Authentication.Extensions;

public static class AuthenticationExtensions
{
    public static void AddAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
    }
}