namespace MambaMQ.Studio.Persistence.Services;

internal sealed class JsonConnectionProfileStorage : IConnectionProfileStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MambaMQ", "Studio", "connection-profiles.json");

    public async Task<IReadOnlyList<ConnectionProfile>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return [];

        await using FileStream stream = File.OpenRead(_filePath);

        return await JsonSerializer.DeserializeAsync<List<ConnectionProfile>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    public async Task SaveAsync(IReadOnlyList<ConnectionProfile> profiles, CancellationToken cancellationToken = default)
    {
        string? directoryPath = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);

        string temporaryPath = $"{_filePath}.tmp";

        try
        {
            await using (FileStream stream = File.Create(temporaryPath))
                await JsonSerializer.SerializeAsync(stream, profiles, JsonOptions, cancellationToken);

            File.Move(temporaryPath, _filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
