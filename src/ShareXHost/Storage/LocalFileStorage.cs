namespace ShareXHost.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _storageRoot;

    public LocalFileStorage(IHostEnvironment environment)
    {
        _storageRoot = Path.Combine(environment.ContentRootPath, "Storage", "Current");
        Directory.CreateDirectory(_storageRoot);
    }
    
    public async Task<string> SaveFileAsync(Stream file)
    {
        string fileName = Path.GetRandomFileName();
        string currentFolder = CurrentFolder();
        string filePath = Path.Combine(currentFolder, fileName);
        await using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
        await file.CopyToAsync(fileStream);
        return filePath;
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

    private string CurrentFolder()
    {
        string yearFolder = Path.Combine(_storageRoot, DateTime.UtcNow.Year.ToString());
        string monthFolder = Path.Combine(yearFolder, DateTime.UtcNow.Month.ToString("D2"));
        string dayFolder = Path.Combine(monthFolder, DateTime.UtcNow.Day.ToString("D2"));
        Directory.CreateDirectory(dayFolder);
        return dayFolder;
    }
}