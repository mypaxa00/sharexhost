namespace ShareXHost.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _storageRoot;

    public LocalFileStorage(IHostEnvironment environment)
    {
        _storageRoot = Path.Combine(environment.ContentRootPath, "storage");
        Directory.CreateDirectory(_storageRoot);
    }
    
    public async Task<string> SaveFileAsync(Stream file)
    {
        string fileName = Path.GetRandomFileName();
        string filePath = Path.Combine(_storageRoot, fileName);
        using (FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(fileStream);
        }
        return fileName;
    }

    public Task<Stream?> GetFileAsync(string storagePath)
    {
        string fullPath = Path.Combine(_storageRoot, storagePath);
        
        // check if we are still within the storage directory to prevent path traversal attacks
        string storageDir = Path.GetFullPath(_storageRoot);
        string requestedPath = Path.GetFullPath(fullPath);
        if (!requestedPath.StartsWith(storageDir) || !System.IO.File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(fileStream);
    }

    public Task DeleteFileAsync(string storagePath)
    {
        string fullPath = Path.Combine(_storageRoot, storagePath);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

        return Task.CompletedTask;
    }
}