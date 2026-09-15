namespace ShareXHost;

public interface IFileService
{
    Task<GetFileResult> GetAsync(Guid fileId);
    Task<UploadFileResult> UploadAsync(Stream content, long sizeBytes, string contentType, Guid? userId);
    Task<DeleteFileResult> DeleteAsync(Guid fileId, string? deleteToken, Guid? userId);
}
