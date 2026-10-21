namespace MambaMQ.Studio.Infrastructure.Services.Implementations;

internal sealed class ConnectionProfileService(
    IConnectionProfileStorage profileStorage) : IConnectionProfileService
{
    public Task<IReadOnlyList<ConnectionProfile>> LoadProfilesAsync(CancellationToken cancellationToken = default)
        => profileStorage.LoadAsync(cancellationToken);

    public async Task SaveProfileAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        List<ConnectionProfile> profiles = (await profileStorage.LoadAsync(cancellationToken)).ToList();
        int index = profiles.FindIndex(item => item.Id == profile.Id);

        if (index < 0)
            profiles.Add(profile);
        else
            profiles[index] = profile;

        await profileStorage.SaveAsync(profiles, cancellationToken);
    }

    public async Task DeleteProfileAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        List<ConnectionProfile> profiles = (await profileStorage.LoadAsync(cancellationToken))
            .Where(item => item.Id != profile.Id)
            .ToList();

        await profileStorage.SaveAsync(profiles, cancellationToken);
    }
}
