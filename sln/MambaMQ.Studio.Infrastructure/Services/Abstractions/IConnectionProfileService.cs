namespace MambaMQ.Studio.Infrastructure.Services.Abstractions;

public interface IConnectionProfileService
{
    Task<IReadOnlyList<ConnectionProfile>> LoadProfilesAsync(CancellationToken cancellationToken = default);
    Task SaveProfileAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task DeleteProfileAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
}