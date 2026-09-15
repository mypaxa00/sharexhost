using Microsoft.EntityFrameworkCore;

namespace ShareXHost;

public sealed class FileService : IFileService
{
    private readonly AppDbContext _dbContext;
    private readonly IFileStorage _storage;
    private readonly ILogger<FileService> _logger;

    public FileService(
        AppDbContext dbContext,
        IFileStorage storage,
        ILogger<FileService> logger)
    {
        _dbContext = dbContext;
        _storage = storage;
        _logger = logger;
    }

    public async Task<GetFileResult> GetAsync(Guid fileId)
    {
        FileData? file = await _dbContext.Files
            .AsNoTracking()
            .Where(x => x.Id == fileId)
            .Select(x => new FileData(x.StoragePath, x.ContentType))
            .FirstOrDefaultAsync();

        if (file is null)
        {
            return GetFileResult.NotFound();
        }

        Stream? stream = await _storage.GetFileAsync(file.StoragePath);
        return stream is null
            ? GetFileResult.StorageFailure()
            : GetFileResult.Success(stream, file.ContentType);
    }

    public async Task<UploadFileResult> UploadAsync(Stream content, long sizeBytes, string contentType, Guid? userId)
    {
        if (sizeBytes == 0)
        {
            return UploadFileResult.Empty();
        }

        string storagePath = await _storage.SaveFileAsync(content);

        File newFile = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StoragePath = storagePath,
            SizeBytes = sizeBytes,
            ContentType = contentType,
            CreatedAt = DateTime.UtcNow,
            DeleteToken = Guid.NewGuid().ToString()
        };

        _dbContext.Files.Add(newFile);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error saving new file to the database.");

            try
            {
                await _storage.DeleteFileAsync(storagePath);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(
                    cleanupException,
                    "Error deleting file from storage after database save failure. " +
                    "Storage path: {StoragePath}. Orphan file may remain in storage.",
                    storagePath);
            }

            return UploadFileResult.Failure("Error saving new file to the database.");
        }

        return UploadFileResult.Success(newFile.Id, userId.HasValue ? null : newFile.DeleteToken);
    }

    public async Task<DeleteFileResult> DeleteAsync(Guid fileId, string? deleteToken, Guid? userId)
    {
        File? file = await _dbContext.Files.FirstOrDefaultAsync(x => x.Id == fileId);
        if (file is null)
        {
            return DeleteFileResult.NoContent();
        }

        bool canDeleteOwnedFile = userId.HasValue && file.UserId == userId;
        bool canDeleteGuestFile = file.UserId is null && file.DeleteToken == deleteToken;
        
        if (canDeleteGuestFile || canDeleteOwnedFile)
        {
            _dbContext.Files.Remove(file);
            try
            {
                await _dbContext.SaveChangesAsync();
                await _storage.DeleteFileAsync(file.StoragePath);
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Error deleting database entry for {FileId}", file.Id);
                return DeleteFileResult.DatabaseFailure();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error deleting file {FileId}. Orphan file may remain in storage.", file.Id);
            }

            return DeleteFileResult.NoContent();
        }

        _logger.LogWarning("Unauthorized delete attempt for file {FileId} by user {UserId}", file.Id, userId);
        return !userId.HasValue
            ? DeleteFileResult.NoContent()
            : DeleteFileResult.Forbidden();
    }

    private sealed record FileData(string StoragePath, string ContentType);
}
