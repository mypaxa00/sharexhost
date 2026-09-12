namespace ShareXHost;

public sealed class UploadFileResponse
{
    public string? Url { get; set; } = null!;
    public string? DeletionUrl { get; set; } = null!;
    public string? Error { get; set; } = null!;
}