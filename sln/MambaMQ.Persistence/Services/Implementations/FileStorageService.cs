namespace MambaMQ.Persistence.Services.Implementations;

internal sealed class FileStorageService : IFileStorageService
{
    private const int BufferSize = 64 * 1024;
    private readonly string _rootPath;
    
    public FileStorageService(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        
        Directory.CreateDirectory(_rootPath);
    }
    
    public async Task AppendToFile(string path, ReadOnlyMemory<byte> data, bool flushToDisk = false, CancellationToken cancellationToken = default)
    {
        string fullPath = GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        
        if (directory is not null)
            Directory.CreateDirectory(directory);
        
        await using FileStream stream = new(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read, bufferSize: BufferSize, useAsync: true);
        
        await stream.WriteAsync(data, cancellationToken);
        
        if (flushToDisk) 
            await stream.FlushAsync(cancellationToken);
    }

    public async Task ReplaceFile(string path, ReadOnlyMemory<byte> data, bool flushToDisk = false, CancellationToken cancellationToken = default)
    {
        string fullPath = GetFullPath(path);
        
        EnsureDirectoryExists(fullPath);
        
        string temporaryPath = $"{fullPath}.{Guid.CreateVersion7():N}.tmp";
        
        try
        {
            await using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: BufferSize, useAsync: true))
            {
                await stream.WriteAsync(data, cancellationToken);
                
                if (flushToDisk) 
                    await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(temporaryPath)) 
                File.Delete(temporaryPath); 
            
            throw;
        }
    }

    public Task<Stream> OpenReadFile(string path)
        => Task.FromResult<Stream>(new FileStream(GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: BufferSize, useAsync: true));

    public Task DeleteFile(string path)
    {
        string fullPath = GetFullPath(path);
        File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<string>> GetDirectories(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string fullPath = GetFullPath(path);
        
        if(!Directory.Exists(fullPath))
            return Task.FromResult<IReadOnlyCollection<string>>([]);
        
        IReadOnlyCollection<string>  directories = Directory
            .EnumerateDirectories(fullPath)
            .Select(Path.GetFileName)
            .Where(static name => name is not null)
            .OfType<string>()
            .ToArray();
        
        return Task.FromResult(directories);
    }

    public Task<IReadOnlyCollection<string>> GetFiles(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = GetFullPath(path);

        if (!Directory.Exists(fullPath))
            return Task.FromResult<IReadOnlyCollection<string>>([]);

        IReadOnlyCollection<string> files = Directory
            .EnumerateFiles(fullPath)
            .Select(file => Path.GetRelativePath(_rootPath, file))
            .ToArray();

        return Task.FromResult(files);
    }
    
    public Task DeleteDirectory(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        string fullPath = GetFullPath(path);
        
        if(Directory.Exists(fullPath))
            Directory.Delete(fullPath, true);
        
        return Task.CompletedTask;
    }

    public DateTime GetLastWriteTimeUtc(string path)
        => File.GetLastWriteTimeUtc(GetFullPath(path));

    private static void EnsureDirectoryExists(string fullPath)
    {
        string? directory = Path.GetDirectoryName(fullPath);
        
        if (directory is not null) 
            Directory.CreateDirectory(directory);
    }
    
    private string GetFullPath(string relativePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

        string rootPath = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        return fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : throw new ArgumentException(
                "Path is outside of the storage root.",
                nameof(relativePath));
    }
}