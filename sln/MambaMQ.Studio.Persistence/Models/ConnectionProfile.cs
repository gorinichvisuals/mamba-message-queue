namespace MambaMQ.Studio.Persistence.Models;

public sealed class ConnectionProfile
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
