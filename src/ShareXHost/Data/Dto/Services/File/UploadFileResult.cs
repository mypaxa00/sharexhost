namespace ShareXHost;

public sealed class UploadFileResult
{
    public UploadFileStatus Status { get; private init; }
    public Guid? FileId { get; private init; }
    public string? DeleteToken { get; private init; }
    public string? Error { get; private init; }

    public static UploadFileResult Empty() => new()
    {
        Status = UploadFileStatus.Empty,
        Error = "File is empty."
    };

    public static UploadFileResult Failure(string error) => new()
    {
        Status = UploadFileStatus.Failure,
        Error = error
    };

    public static UploadFileResult Success(Guid fileId, string? deleteToken) => new()
    {
        Status = UploadFileStatus.Success,
        FileId = fileId,
        DeleteToken = deleteToken
    };
}

public enum UploadFileStatus
{
    Success = 0,
    Empty,
    Failure
}
