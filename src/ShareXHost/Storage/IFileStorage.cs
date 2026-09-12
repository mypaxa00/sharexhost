namespace ShareXHost.Storage;

public interface IFileStorage
{
    Task<string> SaveFileAsync(Stream file);
    Task<Stream?> GetFileAsync(string storagePath);
    Task DeleteFileAsync(string storagePath);
}