namespace MambaMQ.Persistence.Services.Abstractions;

public interface IFileStorageService
{
    Task AppendToFile(string path, ReadOnlyMemory<byte> data, bool flushToDisk = false, CancellationToken cancellationToken  = default);
    Task ReplaceFile(string path, ReadOnlyMemory<byte> data, bool flushToDisk = false, CancellationToken cancellationToken  = default);
    Task<Stream> OpenReadFile(string path);
    Task DeleteFile(string path);
    Task<IReadOnlyCollection<string>> GetDirectories(string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetFiles(string path, CancellationToken cancellationToken = default);
    Task DeleteDirectory(string path, CancellationToken cancellationToken = default);
}