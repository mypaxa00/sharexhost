namespace ShareXHost;

public sealed class GetFileResult
{
    public Stream? Stream { get; private init; }
    public string? ContentType { get; private init; }
    public bool IsNotFound { get; private init; }
    public bool HasStorageFailure { get; private init; }

    public static GetFileResult Success(Stream stream, string contentType) => new()
    {
        Stream = stream,
        ContentType = contentType
    };

    public static GetFileResult NotFound() => new()
    {
        IsNotFound = true
    };

    public static GetFileResult StorageFailure() => new()
    {
        HasStorageFailure = true
    };
}
