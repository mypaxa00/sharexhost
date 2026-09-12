using ShareXHost.Storage;

namespace ShareXHost.Tests;

public sealed class TestFileStorage : IFileStorage
{
    private readonly Dictionary<string, byte[]> _files = new();
    
    public async Task<string> SaveFileAsync(Stream file)
    {
        using MemoryStream memoryStream = new();
        await file.CopyToAsync(memoryStream);
        string storagePath = Guid.NewGuid().ToString();
        _files[storagePath] = memoryStream.ToArray();
        return storagePath;
    }

    public Task<Stream?> GetFileAsync(string storagePath)
    {
        if (_files.TryGetValue(storagePath, out byte[]? fileData))
        {
            Stream stream = new MemoryStream(fileData);
            return Task.FromResult<Stream?>(stream);
        }
        return Task.FromResult<Stream?>(null);
    }

    public Task DeleteFileAsync(string storagePath)
    {
        _files.Remove(storagePath);
        return Task.CompletedTask;
    }
}